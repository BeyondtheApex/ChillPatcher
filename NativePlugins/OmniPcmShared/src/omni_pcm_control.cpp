// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 ChillPatcher Contributors

#include "omni_pcm_shared.h"

#include "httplib.h"
#include "omni_mix_player/events/ws_events.pb.h"
#include "omni_mix_player/models/common.pb.h"
#include "omni_mix_player/models/instance.pb.h"
#include "omni_mix_player/services/instance.pb.h"
#include "omni_mix_player/services/library.pb.h"
#include "omni_mix_player/services/playback.pb.h"

#include <atomic>
#include <chrono>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <memory>
#include <sstream>
#include <string>
#include <thread>
#include <vector>

#ifdef _WIN32
#include <windows.h>
#include <winhttp.h>
#endif

namespace {

using google::protobuf::MessageLite;

constexpr int DEFAULT_PORT = 17890;
constexpr int DEFAULT_TIMEOUT_MS = 3000;

struct OmniPcmClientContext {
    std::string host = "127.0.0.1";
    int port = DEFAULT_PORT;
    int timeout_ms = DEFAULT_TIMEOUT_MS;
    std::string last_error;
    std::atomic<bool> event_stop{false};
    std::thread event_thread;

    void set_error(const std::string& message) {
        last_error = message;
    }
};

OmniPcmClientContext* client_ctx(OmniPcmClientHandle handle) {
    return static_cast<OmniPcmClientContext*>(handle);
}

void copy_text(char* dst, size_t dst_size, const std::string& value) {
    if (!dst || dst_size == 0) return;
    std::memset(dst, 0, dst_size);
    if (!value.empty()) {
        std::strncpy(dst, value.c_str(), dst_size - 1);
    }
}

std::string read_file_trimmed(const std::string& path) {
    std::ifstream in(path, std::ios::binary);
    if (!in) return {};
    std::ostringstream ss;
    ss << in.rdbuf();
    auto text = ss.str();
    while (!text.empty() && (text.back() == '\r' || text.back() == '\n' || text.back() == ' ' || text.back() == '\t')) {
        text.pop_back();
    }
    return text;
}

int discover_port() {
    std::vector<std::string> paths;
#ifdef _WIN32
    char public_dir[MAX_PATH]{};
    if (GetEnvironmentVariableA("PUBLIC", public_dir, MAX_PATH) > 0) {
        paths.emplace_back(std::string(public_dir) + "\\OmniMixPlayer\\omnimix_port.txt");
    }
#endif
    paths.emplace_back("omnimix_port.txt");

    for (const auto& path : paths) {
        auto text = read_file_trimmed(path);
        if (text.empty()) continue;
        char* end = nullptr;
        long value = std::strtol(text.c_str(), &end, 10);
        if (end != text.c_str() && value > 0 && value <= 65535) {
            return static_cast<int>(value);
        }
    }
    return DEFAULT_PORT;
}

std::string grpc_frame(const MessageLite& message) {
    std::string payload;
    message.SerializeToString(&payload);
    std::string framed;
    framed.resize(5 + payload.size());
    framed[0] = 0;
    uint32_t len = static_cast<uint32_t>(payload.size());
    framed[1] = static_cast<char>((len >> 24) & 0xff);
    framed[2] = static_cast<char>((len >> 16) & 0xff);
    framed[3] = static_cast<char>((len >> 8) & 0xff);
    framed[4] = static_cast<char>(len & 0xff);
    std::memcpy(framed.data() + 5, payload.data(), payload.size());
    return framed;
}

bool parse_grpc_web_response(const std::string& body, MessageLite* out, std::string* error) {
    size_t offset = 0;
    while (offset + 5 <= body.size()) {
        uint8_t flags = static_cast<uint8_t>(body[offset]);
        uint32_t len =
            (static_cast<uint8_t>(body[offset + 1]) << 24) |
            (static_cast<uint8_t>(body[offset + 2]) << 16) |
            (static_cast<uint8_t>(body[offset + 3]) << 8) |
            static_cast<uint8_t>(body[offset + 4]);
        offset += 5;
        if (offset + len > body.size()) {
            if (error) *error = "Malformed gRPC-Web response frame";
            return false;
        }
        if ((flags & 0x80) == 0) {
            if (!out || out->ParseFromArray(body.data() + offset, static_cast<int>(len))) {
                return true;
            }
            if (error) *error = "Failed to parse protobuf response";
            return false;
        }
        offset += len;
    }

    if (out && out->ParseFromString(body)) {
        return true;
    }
    if (error) *error = "No protobuf message in gRPC-Web response";
    return false;
}

std::string json_string_value(const std::string& json, const char* key) {
    std::string needle = "\"";
    needle += key;
    needle += "\"";
    size_t pos = json.find(needle);
    if (pos == std::string::npos) return {};
    pos = json.find(':', pos + needle.size());
    if (pos == std::string::npos) return {};
    pos = json.find('"', pos + 1);
    if (pos == std::string::npos) return {};
    size_t end = json.find('"', pos + 1);
    if (end == std::string::npos) return {};
    return json.substr(pos + 1, end - pos - 1);
}

int64_t json_int64_value(const std::string& json, const char* key) {
    std::string needle = "\"";
    needle += key;
    needle += "\"";
    size_t pos = json.find(needle);
    if (pos == std::string::npos) return 0;
    pos = json.find(':', pos + needle.size());
    if (pos == std::string::npos) return 0;
    char* end = nullptr;
    return std::strtoll(json.c_str() + pos + 1, &end, 10);
}

httplib::Client make_http_client(OmniPcmClientContext* c) {
    httplib::Client http(c->host, c->port);
    http.set_connection_timeout(0, c->timeout_ms * 1000);
    http.set_read_timeout(0, c->timeout_ms * 1000);
    http.set_write_timeout(0, c->timeout_ms * 1000);
    return http;
}

int grpc_web_unary(
    OmniPcmClientContext* c,
    const char* service,
    const char* method,
    const MessageLite& request,
    MessageLite* response) {
    if (!c || !service || !method) return OMNI_PCM_BAD_ARGUMENT;

    auto http = make_http_client(c);

    httplib::Headers headers{
        {"content-type", "application/grpc-web+proto"},
        {"x-grpc-web", "1"},
        {"x-user-agent", "omni-pcm-shared"}
    };

    std::string path = "/";
    path += service;
    path += "/";
    path += method;

    auto result = http.Post(path, headers, grpc_frame(request), "application/grpc-web+proto");
    if (!result) {
        c->set_error("HTTP request failed");
        return OMNI_PCM_ERROR;
    }
    if (result->status < 200 || result->status >= 300) {
        c->set_error("HTTP status " + std::to_string(result->status) + ": " + result->body);
        return OMNI_PCM_ERROR;
    }

    std::string parse_error;
    if (response && !parse_grpc_web_response(result->body, response, &parse_error)) {
        c->set_error(parse_error);
        return OMNI_PCM_ERROR;
    }
    return OMNI_PCM_OK;
}

void fill_capabilities(uint32_t flags, omni_mix_player::InstanceCapabilities* caps) {
    if (!caps) return;
    caps->set_server_controlled_playback((flags & OMNI_PCM_CAP_SERVER_CONTROLLED_PLAYBACK) != 0);
    caps->set_queue_management((flags & OMNI_PCM_CAP_QUEUE_MANAGEMENT) != 0);
    caps->set_playlist_management((flags & OMNI_PCM_CAP_PLAYLIST_MANAGEMENT) != 0);
    caps->set_shuffle((flags & OMNI_PCM_CAP_SHUFFLE) != 0);
    caps->set_repeat((flags & OMNI_PCM_CAP_REPEAT) != 0);
    caps->set_seek((flags & OMNI_PCM_CAP_SEEK) != 0);
    caps->set_volume_control((flags & OMNI_PCM_CAP_VOLUME_CONTROL) != 0);
    caps->set_equalizer((flags & OMNI_PCM_CAP_EQUALIZER) != 0);
    caps->set_multiple_playlists((flags & OMNI_PCM_CAP_MULTIPLE_PLAYLISTS) != 0);
    caps->set_tag_filtering((flags & OMNI_PCM_CAP_TAG_FILTERING) != 0);
    caps->set_unlimited_tags((flags & OMNI_PCM_CAP_UNLIMITED_TAGS) != 0);
    caps->set_album_filtering((flags & OMNI_PCM_CAP_ALBUM_FILTERING) != 0);
    caps->set_audio_playback((flags & OMNI_PCM_CAP_AUDIO_PLAYBACK) != 0);
    caps->set_custom_system_media_service((flags & OMNI_PCM_CAP_CUSTOM_SYSTEM_MEDIA_SERVICE) != 0);
}

uint32_t capability_flags(const omni_mix_player::InstanceCapabilities& caps) {
    uint32_t flags = 0;
    if (caps.server_controlled_playback()) flags |= OMNI_PCM_CAP_SERVER_CONTROLLED_PLAYBACK;
    if (caps.queue_management()) flags |= OMNI_PCM_CAP_QUEUE_MANAGEMENT;
    if (caps.playlist_management()) flags |= OMNI_PCM_CAP_PLAYLIST_MANAGEMENT;
    if (caps.shuffle()) flags |= OMNI_PCM_CAP_SHUFFLE;
    if (caps.repeat()) flags |= OMNI_PCM_CAP_REPEAT;
    if (caps.seek()) flags |= OMNI_PCM_CAP_SEEK;
    if (caps.volume_control()) flags |= OMNI_PCM_CAP_VOLUME_CONTROL;
    if (caps.equalizer()) flags |= OMNI_PCM_CAP_EQUALIZER;
    if (caps.multiple_playlists()) flags |= OMNI_PCM_CAP_MULTIPLE_PLAYLISTS;
    if (caps.tag_filtering()) flags |= OMNI_PCM_CAP_TAG_FILTERING;
    if (caps.unlimited_tags()) flags |= OMNI_PCM_CAP_UNLIMITED_TAGS;
    if (caps.album_filtering()) flags |= OMNI_PCM_CAP_ALBUM_FILTERING;
    if (caps.audio_playback()) flags |= OMNI_PCM_CAP_AUDIO_PLAYBACK;
    if (caps.custom_system_media_service()) flags |= OMNI_PCM_CAP_CUSTOM_SYSTEM_MEDIA_SERVICE;
    return flags;
}

void fill_profile(const omni_mix_player::InstanceProfile& profile, OmniPcmInstanceProfileInfo* out) {
    if (!out) return;
    std::memset(out, 0, sizeof(*out));
    copy_text(out->instance_id, sizeof(out->instance_id), profile.id());
    copy_text(out->display_name, sizeof(out->display_name), profile.display_name());
    copy_text(out->mod_id, sizeof(out->mod_id), profile.mod_id());
    copy_text(out->game_name, sizeof(out->game_name), profile.game_name());
    out->kind = static_cast<int32_t>(profile.kind());
    out->capability_flags = capability_flags(profile.capabilities());
    out->volume = profile.volume();
    out->target_latency = profile.target_latency();
    out->created_at = profile.has_created_at() ? profile.created_at().seconds() : 0;
    out->updated_at = profile.has_updated_at() ? profile.updated_at().seconds() : 0;
}

void fill_summary(const omni_mix_player::InstanceSummary& summary, OmniPcmInstanceSummaryInfo* out) {
    if (!out) return;
    std::memset(out, 0, sizeof(*out));
    copy_text(out->instance_id, sizeof(out->instance_id), summary.id());
    copy_text(out->display_name, sizeof(out->display_name), summary.display_name());
    copy_text(out->mod_id, sizeof(out->mod_id), summary.mod_id());
    copy_text(out->game_name, sizeof(out->game_name), summary.game_name());
    copy_text(out->current_track_uuid, sizeof(out->current_track_uuid), summary.current_track_uuid());
    out->kind = static_cast<int32_t>(summary.kind());
    out->is_online = summary.is_online() ? 1 : 0;
    out->queue_count = summary.queue_count();
    out->connected_at = summary.has_connected_at() ? summary.connected_at().seconds() : 0;
}

void fill_queue_track(const omni_mix_player::QueueTrack& track, OmniPcmQueueTrackInfo* out) {
    if (!out) return;
    std::memset(out, 0, sizeof(*out));
    out->index = track.index();
    copy_text(out->uuid, sizeof(out->uuid), track.uuid());
    copy_text(out->title, sizeof(out->title), track.title());
    copy_text(out->artist, sizeof(out->artist), track.artist());
    copy_text(out->album_id, sizeof(out->album_id), track.album_id());
    copy_text(out->module_id, sizeof(out->module_id), track.module_id());
    copy_text(out->cover_uri, sizeof(out->cover_uri), track.cover_uri());
    out->duration = track.duration();
}

void fill_playlist_source(const omni_mix_player::PlaylistSourceInfo& source, OmniPcmPlaylistSourceInfo* out) {
    if (!out) return;
    std::memset(out, 0, sizeof(*out));
    copy_text(out->id, sizeof(out->id), source.id());
    copy_text(out->name, sizeof(out->name), source.name());
    copy_text(out->ref_id, sizeof(out->ref_id), source.ref_id());
    out->song_count = source.song_count();
    out->kind = static_cast<int32_t>(source.kind());
}

void fill_equalizer_point(const omni_mix_player::EqualizerPoint& point, OmniPcmEqualizerPointInfo* out) {
    if (!out) return;
    std::memset(out, 0, sizeof(*out));
    copy_text(out->id, sizeof(out->id), point.id());
    out->frequency = point.frequency();
    out->gain_db = point.gain_db();
    out->q = point.q();
    out->type = static_cast<int32_t>(point.type());
}

void fill_status(const omni_mix_player::PlaybackStatus& status, OmniPcmPlaybackStatusInfo* out) {
    if (!out) return;
    std::memset(out, 0, sizeof(*out));
    copy_text(out->track_uuid, sizeof(out->track_uuid), status.track_uuid());
    copy_text(out->title, sizeof(out->title), status.title());
    copy_text(out->artist, sizeof(out->artist), status.artist());
    copy_text(out->album_id, sizeof(out->album_id), status.album_id());
    out->duration = status.duration();
    out->position = status.position();
    out->is_playing = status.is_playing() ? 1 : 0;
    out->shuffle = status.shuffle() ? 1 : 0;
    out->repeat_mode = static_cast<int32_t>(status.repeat_mode());
    out->volume = status.volume();
}

void fill_event(const omni_mix_player::WsEvent& evt, OmniPcmEventInfo* out) {
    std::memset(out, 0, sizeof(*out));
    copy_text(out->type, sizeof(out->type), evt.type());
    out->timestamp = evt.timestamp();

    switch (evt.event_case()) {
        case omni_mix_player::WsEvent::kTrackChanged: {
            const auto& e = evt.track_changed();
            copy_text(out->instance_id, sizeof(out->instance_id), e.instance_id());
            copy_text(out->track_uuid, sizeof(out->track_uuid), e.uuid());
            copy_text(out->title, sizeof(out->title), e.title());
            copy_text(out->artist, sizeof(out->artist), e.artist());
            copy_text(out->album_id, sizeof(out->album_id), e.album_id());
            copy_text(out->module_id, sizeof(out->module_id), e.module_id());
            out->duration = e.duration();
            break;
        }
        case omni_mix_player::WsEvent::kStateChanged:
            copy_text(out->instance_id, sizeof(out->instance_id), evt.state_changed().instance_id());
            out->state = evt.state_changed().state();
            break;
        case omni_mix_player::WsEvent::kPositionChanged:
            copy_text(out->instance_id, sizeof(out->instance_id), evt.position_changed().instance_id());
            out->position = evt.position_changed().position();
            break;
        case omni_mix_player::WsEvent::kQueueChanged:
            copy_text(out->instance_id, sizeof(out->instance_id), evt.queue_changed().instance_id());
            copy_text(out->change_type, sizeof(out->change_type), evt.queue_changed().change_type());
            out->queue_length = evt.queue_changed().queue_length();
            break;
        case omni_mix_player::WsEvent::kInstancesChanged:
            out->instance_count = evt.instances_changed().instances_size();
            break;
        case omni_mix_player::WsEvent::kFavoriteChanged:
            copy_text(out->track_uuid, sizeof(out->track_uuid), evt.favorite_changed().uuid());
            copy_text(out->module_id, sizeof(out->module_id), evt.favorite_changed().module_id());
            out->bool_value = evt.favorite_changed().is_favorite() ? 1 : 0;
            break;
        case omni_mix_player::WsEvent::kExcludeChanged:
            copy_text(out->track_uuid, sizeof(out->track_uuid), evt.exclude_changed().uuid());
            copy_text(out->module_id, sizeof(out->module_id), evt.exclude_changed().module_id());
            out->bool_value = evt.exclude_changed().is_excluded() ? 1 : 0;
            break;
        case omni_mix_player::WsEvent::kPlaylistUpdated:
            copy_text(out->source_ref_id, sizeof(out->source_ref_id), evt.playlist_updated().source_ref_id());
            copy_text(out->change_type, sizeof(out->change_type), evt.playlist_updated().update_type());
            out->song_count = evt.playlist_updated().song_count();
            break;
        case omni_mix_player::WsEvent::kModuleChanged:
            copy_text(out->module_id, sizeof(out->module_id), evt.module_changed().module_id());
            copy_text(out->display_name, sizeof(out->display_name), evt.module_changed().display_name());
            out->bool_value = evt.module_changed().enabled() ? 1 : 0;
            break;
        case omni_mix_player::WsEvent::kProfileChanged:
            copy_text(out->instance_id, sizeof(out->instance_id), evt.profile_changed().instance_id());
            break;
        case omni_mix_player::WsEvent::kBackendState:
            out->backend_running = evt.backend_state().running() ? 1 : 0;
            break;
        default:
            break;
    }
}

#ifdef _WIN32
std::wstring widen_ascii(const std::string& text) {
    return std::wstring(text.begin(), text.end());
}

void websocket_event_loop(OmniPcmClientContext* c, OmniPcmEventCallback callback, void* user_data) {
    HINTERNET session = WinHttpOpen(L"OmniPcmShared/1.0", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
        WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
    if (!session) {
        c->set_error("WinHttpOpen failed");
        return;
    }
    DWORD receive_timeout = 1000;
    WinHttpSetOption(session, WINHTTP_OPTION_RECEIVE_TIMEOUT, &receive_timeout, sizeof(receive_timeout));

    HINTERNET connect = WinHttpConnect(session, widen_ascii(c->host).c_str(),
        static_cast<INTERNET_PORT>(c->port), 0);
    if (!connect) {
        c->set_error("WinHttpConnect failed");
        WinHttpCloseHandle(session);
        return;
    }

    HINTERNET request = WinHttpOpenRequest(connect, L"GET", L"/ws", nullptr,
        WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, 0);
    if (!request) {
        c->set_error("WinHttpOpenRequest failed");
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return;
    }

    DWORD zero = 0;
    WinHttpSetOption(request, WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET, &zero, sizeof(zero));
    if (!WinHttpSendRequest(request, WINHTTP_NO_ADDITIONAL_HEADERS, 0, nullptr, 0, 0, 0) ||
        !WinHttpReceiveResponse(request, nullptr)) {
        c->set_error("WebSocket upgrade failed");
        WinHttpCloseHandle(request);
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return;
    }

    HINTERNET ws = WinHttpWebSocketCompleteUpgrade(request, 0);
    WinHttpCloseHandle(request);
    if (!ws) {
        c->set_error("WinHttpWebSocketCompleteUpgrade failed");
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        return;
    }

    std::vector<uint8_t> chunk(64 * 1024);
    std::string message;
    while (!c->event_stop.load()) {
        DWORD read = 0;
        WINHTTP_WEB_SOCKET_BUFFER_TYPE type{};
        DWORD status = WinHttpWebSocketReceive(ws, chunk.data(), static_cast<DWORD>(chunk.size()), &read, &type);
        if (status == ERROR_WINHTTP_TIMEOUT) {
            continue;
        }
        if (status != NO_ERROR) {
            c->set_error("WinHttpWebSocketReceive failed");
            break;
        }
        if (type == WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE) {
            break;
        }
        if (type == WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE ||
            type == WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE) {
            message.append(reinterpret_cast<const char*>(chunk.data()), read);
            if (type == WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE) {
                omni_mix_player::WsEvent evt;
                if (evt.ParseFromString(message)) {
                    OmniPcmEventInfo info{};
                    fill_event(evt, &info);
                    callback(&info, user_data);
                }
                message.clear();
            }
        }
    }

    WinHttpWebSocketClose(ws, WINHTTP_WEB_SOCKET_SUCCESS_CLOSE_STATUS, nullptr, 0);
    WinHttpCloseHandle(ws);
    WinHttpCloseHandle(connect);
    WinHttpCloseHandle(session);
}
#endif

template <typename Request, typename Response>
int unary_playback(OmniPcmClientContext* c, const char* method, const char* instance_id) {
    if (!instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    Request req;
    req.set_instance_id(instance_id);
    Response resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", method, req, &resp);
}

template <typename Repeated>
int fill_counted_buffer(int total, Repeated&& fill, int32_t* inout_count) {
    if (!inout_count) return OMNI_PCM_BAD_ARGUMENT;
    int32_t capacity = *inout_count;
    *inout_count = total;
    if (capacity < 0) return OMNI_PCM_BAD_ARGUMENT;
    for (int32_t i = 0; i < capacity && i < total; ++i) {
        fill(i);
    }
    return capacity >= total ? OMNI_PCM_OK : OMNI_PCM_NOT_READY;
}

} // namespace

OMNI_PCM_API OmniPcmClientHandle OmniPcmClient_Create(const OmniPcmClientConfig* config) {
    auto* c = new OmniPcmClientContext();
    if (config) {
        if (config->host && *config->host) c->host = config->host;
        c->port = config->port > 0 ? config->port : discover_port();
        c->timeout_ms = config->timeout_ms > 0 ? config->timeout_ms : DEFAULT_TIMEOUT_MS;
    } else {
        c->port = discover_port();
    }
    return c;
}

OMNI_PCM_API void OmniPcmClient_Destroy(OmniPcmClientHandle client) {
    auto* c = client_ctx(client);
    if (!c) return;
    OmniPcmClient_StopEvents(client);
    delete c;
}

OMNI_PCM_API const char* OmniPcmClient_GetLastError(OmniPcmClientHandle client) {
    auto* c = client_ctx(client);
    return c ? c->last_error.c_str() : "Invalid client handle";
}

OMNI_PCM_API int32_t OmniPcmClient_GetPort(OmniPcmClientHandle client) {
    auto* c = client_ctx(client);
    return c ? c->port : 0;
}

OMNI_PCM_API int OmniPcmClient_ConnectInstance(
    OmniPcmClientHandle client,
    const OmniPcmConnectOptions* options,
    OmniPcmConnectionInfo* out_info) {
    auto* c = client_ctx(client);
    if (!c || !options || !options->client_id || !out_info) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::InstanceConnectRequest req;
    req.set_client_id(options->client_id);
    req.set_kind(static_cast<omni_mix_player::InstanceKind>(
        options->kind > 0 ? options->kind : OMNI_PCM_INSTANCE_KIND_GAME_MOD));
    if (options->mod_id) req.set_mod_id(options->mod_id);
    if (options->game_name) req.set_game_name(options->game_name);
    if (options->display_name) req.set_display_name(options->display_name);
    req.set_no_instance(options->no_instance != 0);
    fill_capabilities(options->capability_flags, req.mutable_capabilities());

    omni_mix_player::InstanceConnectResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "Connect", req, &resp);
    if (r != OMNI_PCM_OK) return r;

    std::memset(out_info, 0, sizeof(*out_info));
    copy_text(out_info->instance_id, sizeof(out_info->instance_id), resp.instance_id());
    out_info->is_new = resp.is_new() ? 1 : 0;
    out_info->no_instance = resp.no_instance() ? 1 : 0;
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcmClient_Heartbeat(OmniPcmClientHandle client, const char* instance_id, int* out_alive) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::InstanceHeartbeatRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::InstanceHeartbeatResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "Heartbeat", req, &resp);
    if (r == OMNI_PCM_OK && out_alive) *out_alive = resp.alive() ? 1 : 0;
    return r;
}

OMNI_PCM_API int OmniPcmClient_DisconnectInstance(OmniPcmClientHandle client, const char* instance_id) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::InstanceDisconnectRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::InstanceDisconnectResponse resp;
    return grpc_web_unary(c, "omni_mix_player.InstanceService", "Disconnect", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_DeleteInstance(OmniPcmClientHandle client, const char* instance_id, int* out_deleted) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::DeleteInstanceRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::DeleteInstanceResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "DeleteInstance", req, &resp);
    if (r == OMNI_PCM_OK && out_deleted) *out_deleted = resp.deleted() ? 1 : 0;
    return r;
}

OMNI_PCM_API int OmniPcmClient_ListInstances(
    OmniPcmClientHandle client,
    OmniPcmInstanceSummaryInfo* out_instances,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !inout_count) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::ListInstancesRequest req;
    omni_mix_player::ListInstancesResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "ListInstances", req, &resp);
    if (r != OMNI_PCM_OK) return r;
    return fill_counted_buffer(resp.instances_size(), [&](int32_t i) {
        if (out_instances) fill_summary(resp.instances(i), &out_instances[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_GetProfile(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmInstanceProfileInfo* out_profile) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !out_profile) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetProfileRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::InstanceProfile resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "GetProfile", req, &resp);
    if (r == OMNI_PCM_OK) fill_profile(resp, out_profile);
    return r;
}

OMNI_PCM_API int OmniPcmClient_UpdateProfile(
    OmniPcmClientHandle client,
    const OmniPcmInstanceProfileInfo* profile,
    int* out_saved) {
    auto* c = client_ctx(client);
    if (!c || !profile || !profile->instance_id[0]) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::GetProfileRequest get_req;
    get_req.set_instance_id(profile->instance_id);
    omni_mix_player::InstanceProfile existing;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "GetProfile", get_req, &existing);
    if (r != OMNI_PCM_OK) return r;

    existing.set_id(profile->instance_id);
    existing.set_display_name(profile->display_name);
    existing.set_mod_id(profile->mod_id);
    existing.set_game_name(profile->game_name);
    existing.set_kind(static_cast<omni_mix_player::InstanceKind>(profile->kind));
    existing.set_volume(profile->volume);
    existing.set_target_latency(profile->target_latency);
    fill_capabilities(profile->capability_flags, existing.mutable_capabilities());

    omni_mix_player::UpdateProfileRequest req;
    req.set_instance_id(profile->instance_id);
    *req.mutable_profile() = existing;
    omni_mix_player::UpdateProfileResponse resp;
    r = grpc_web_unary(c, "omni_mix_player.InstanceService", "UpdateProfile", req, &resp);
    if (r == OMNI_PCM_OK && out_saved) *out_saved = resp.saved() ? 1 : 0;
    return r;
}

OMNI_PCM_API int OmniPcmClient_ArchiveInstance(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* label,
    OmniPcmInstanceProfileInfo* out_archive) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::ArchiveInstanceRequest req;
    req.set_instance_id(instance_id);
    if (label) req.set_label(label);
    omni_mix_player::ArchiveInstanceResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "ArchiveInstance", req, &resp);
    if (r == OMNI_PCM_OK && out_archive) fill_profile(resp.archive(), out_archive);
    return r;
}

OMNI_PCM_API int OmniPcmClient_ListArchives(
    OmniPcmClientHandle client,
    OmniPcmInstanceProfileInfo* out_archives,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !inout_count) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::ListArchivesRequest req;
    omni_mix_player::ListArchivesResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "ListArchives", req, &resp);
    if (r != OMNI_PCM_OK) return r;
    return fill_counted_buffer(resp.archives_size(), [&](int32_t i) {
        if (out_archives) fill_profile(resp.archives(i), &out_archives[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_GetArchive(
    OmniPcmClientHandle client,
    const char* archive_id,
    OmniPcmInstanceProfileInfo* out_archive) {
    auto* c = client_ctx(client);
    if (!c || !archive_id || !*archive_id || !out_archive) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetArchiveRequest req;
    req.set_archive_id(archive_id);
    omni_mix_player::InstanceProfile resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "GetArchive", req, &resp);
    if (r == OMNI_PCM_OK) fill_profile(resp, out_archive);
    return r;
}

OMNI_PCM_API int OmniPcmClient_DeleteArchive(OmniPcmClientHandle client, const char* archive_id, int* out_deleted) {
    auto* c = client_ctx(client);
    if (!c || !archive_id || !*archive_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::DeleteArchiveRequest req;
    req.set_archive_id(archive_id);
    omni_mix_player::DeleteArchiveResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "DeleteArchive", req, &resp);
    if (r == OMNI_PCM_OK && out_deleted) *out_deleted = resp.deleted() ? 1 : 0;
    return r;
}

OMNI_PCM_API int OmniPcmClient_InheritFromArchive(
    OmniPcmClientHandle client,
    const char* new_instance_id,
    const char* archive_id,
    OmniPcmInstanceProfileInfo* out_profile) {
    auto* c = client_ctx(client);
    if (!c || !new_instance_id || !*new_instance_id || !archive_id || !*archive_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::InheritFromArchiveRequest req;
    req.set_new_instance_id(new_instance_id);
    req.set_archive_id(archive_id);
    omni_mix_player::InheritFromArchiveResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.InstanceService", "InheritFromArchive", req, &resp);
    if (r == OMNI_PCM_OK && out_profile) fill_profile(resp.profile(), out_profile);
    return r;
}

OMNI_PCM_API int OmniPcmClient_GetStatus(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmPlaybackStatusInfo* out_status) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !out_status) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetStatusRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::PlaybackStatus resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetStatus", req, &resp);
    if (r == OMNI_PCM_OK) fill_status(resp, out_status);
    return r;
}

OMNI_PCM_API int OmniPcmClient_PlaybackCommand(
    OmniPcmClientHandle client,
    const char* instance_id,
    int32_t command) {
    auto* c = client_ctx(client);
    switch (command) {
        case OMNI_PCM_COMMAND_PAUSE:
            return unary_playback<omni_mix_player::PauseRequest, omni_mix_player::PauseResponse>(c, "Pause", instance_id);
        case OMNI_PCM_COMMAND_RESUME:
            return unary_playback<omni_mix_player::ResumeRequest, omni_mix_player::ResumeResponse>(c, "Resume", instance_id);
        case OMNI_PCM_COMMAND_TOGGLE:
            return unary_playback<omni_mix_player::ToggleRequest, omni_mix_player::ToggleResponse>(c, "Toggle", instance_id);
        case OMNI_PCM_COMMAND_NEXT:
            return unary_playback<omni_mix_player::NextRequest, omni_mix_player::NextResponse>(c, "Next", instance_id);
        case OMNI_PCM_COMMAND_PREV:
            return unary_playback<omni_mix_player::PrevRequest, omni_mix_player::PrevResponse>(c, "Prev", instance_id);
        case OMNI_PCM_COMMAND_STOP:
            return unary_playback<omni_mix_player::StopRequest, omni_mix_player::StopResponse>(c, "Stop", instance_id);
        case OMNI_PCM_COMMAND_PLAY:
            return OmniPcmClient_Play(client, instance_id, nullptr);
        default:
            return OMNI_PCM_BAD_ARGUMENT;
    }
}

OMNI_PCM_API int OmniPcmClient_Play(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* track_uuid) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::PlayRequest req;
    req.set_instance_id(instance_id);
    if (track_uuid) req.set_uuid(track_uuid);
    omni_mix_player::PlayResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "Play", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_Seek(OmniPcmClientHandle client, const char* instance_id, float position_seconds) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SeekRequest req;
    req.set_instance_id(instance_id);
    req.set_position(position_seconds);
    omni_mix_player::SeekResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "Seek", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_SetVolume(OmniPcmClientHandle client, const char* instance_id, float volume) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetVolumeRequest req;
    req.set_instance_id(instance_id);
    req.set_volume(volume);
    omni_mix_player::SetVolumeResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetVolume", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_GetVolume(OmniPcmClientHandle client, const char* instance_id, float* out_volume) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !out_volume) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetVolumeRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::GetVolumeResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetVolume", req, &resp);
    if (r == OMNI_PCM_OK) *out_volume = resp.volume();
    return r;
}

OMNI_PCM_API int OmniPcmClient_SetTargetLatency(OmniPcmClientHandle client, const char* instance_id, float latency) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetTargetLatencyRequest req;
    req.set_instance_id(instance_id);
    req.set_latency(latency);
    omni_mix_player::SetTargetLatencyResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetTargetLatency", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_GetTargetLatency(OmniPcmClientHandle client, const char* instance_id, float* out_latency) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !out_latency) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetTargetLatencyRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::GetTargetLatencyResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetTargetLatency", req, &resp);
    if (r == OMNI_PCM_OK) *out_latency = resp.latency();
    return r;
}

OMNI_PCM_API int OmniPcmClient_SetShuffle(OmniPcmClientHandle client, const char* instance_id, int enabled) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetShuffleRequest req;
    req.set_instance_id(instance_id);
    req.set_enabled(enabled != 0);
    omni_mix_player::SetShuffleResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetShuffle", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_SetRepeatMode(OmniPcmClientHandle client, const char* instance_id, int32_t repeat_mode) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetRepeatModeRequest req;
    req.set_instance_id(instance_id);
    req.set_mode(static_cast<omni_mix_player::RepeatMode>(repeat_mode));
    omni_mix_player::SetRepeatModeResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetRepeatMode", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_GetQueue(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmQueueTrackInfo* out_tracks,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !inout_count) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetQueueRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::GetQueueResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetQueue", req, &resp);
    if (r != OMNI_PCM_OK) return r;
    return fill_counted_buffer(resp.queue_size(), [&](int32_t i) {
        if (out_tracks) fill_queue_track(resp.queue(i), &out_tracks[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_AddToQueue(OmniPcmClientHandle client, const char* instance_id, const char* uuid) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !uuid || !*uuid) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::AddToQueueRequest req;
    req.set_instance_id(instance_id);
    req.set_uuid(uuid);
    omni_mix_player::AddToQueueResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "AddToQueue", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_InsertIntoQueue(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* const* uuids,
    int32_t uuid_count,
    int32_t index) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || uuid_count < 0 || (uuid_count > 0 && !uuids)) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::InsertIntoQueueRequest req;
    req.set_instance_id(instance_id);
    req.set_index(index);
    for (int32_t i = 0; i < uuid_count; ++i) if (uuids[i]) req.add_uuids(uuids[i]);
    omni_mix_player::InsertIntoQueueResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "InsertIntoQueue", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_SetQueue(
    OmniPcmClientHandle client,
    const char* instance_id,
    const char* const* uuids,
    int32_t uuid_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || uuid_count < 0 || (uuid_count > 0 && !uuids)) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetQueueRequest req;
    req.set_instance_id(instance_id);
    for (int32_t i = 0; i < uuid_count; ++i) if (uuids[i]) req.add_uuids(uuids[i]);
    omni_mix_player::SetQueueResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetQueue", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_RemoveFromQueueIndex(OmniPcmClientHandle client, const char* instance_id, int32_t index) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::RemoveFromQueueRequest req;
    req.set_instance_id(instance_id);
    req.set_index(index);
    omni_mix_player::RemoveFromQueueResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "RemoveFromQueue", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_RemoveFromQueueUuid(OmniPcmClientHandle client, const char* instance_id, const char* uuid) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !uuid || !*uuid) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::RemoveFromQueueRequest req;
    req.set_instance_id(instance_id);
    req.set_uuid(uuid);
    omni_mix_player::RemoveFromQueueResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "RemoveFromQueue", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_MoveInQueue(OmniPcmClientHandle client, const char* instance_id, int32_t from_index, int32_t to_index) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::MoveInQueueRequest req;
    req.set_instance_id(instance_id);
    req.set_from_index(from_index);
    req.set_to_index(to_index);
    omni_mix_player::MoveInQueueResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "MoveInQueue", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_ClearQueue(OmniPcmClientHandle client, const char* instance_id) {
    return unary_playback<omni_mix_player::ClearQueueRequest, omni_mix_player::ClearQueueResponse>(
        client_ctx(client), "ClearQueue", instance_id);
}

OMNI_PCM_API int OmniPcmClient_GetHistory(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmQueueTrackInfo* out_tracks,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !inout_count) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetHistoryRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::GetHistoryResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetHistory", req, &resp);
    if (r != OMNI_PCM_OK) return r;
    return fill_counted_buffer(resp.history_size(), [&](int32_t i) {
        if (out_tracks) fill_queue_track(resp.history(i), &out_tracks[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_RemoveFromHistory(OmniPcmClientHandle client, const char* instance_id, int32_t index) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::RemoveFromHistoryRequest req;
    req.set_instance_id(instance_id);
    req.set_index(index);
    omni_mix_player::RemoveFromHistoryResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "RemoveFromHistory", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_MoveInHistory(OmniPcmClientHandle client, const char* instance_id, int32_t from_index, int32_t to_index) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::MoveInHistoryRequest req;
    req.set_instance_id(instance_id);
    req.set_from_index(from_index);
    req.set_to_index(to_index);
    omni_mix_player::MoveInHistoryResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "MoveInHistory", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_ClearHistory(OmniPcmClientHandle client, const char* instance_id) {
    return unary_playback<omni_mix_player::ClearHistoryRequest, omni_mix_player::ClearHistoryResponse>(
        client_ctx(client), "ClearHistory", instance_id);
}

OMNI_PCM_API int OmniPcmClient_GetPlaylistSources(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmPlaylistSourceInfo* out_sources,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !inout_count) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetPlaylistSourcesRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::GetPlaylistSourcesResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetPlaylistSources", req, &resp);
    if (r != OMNI_PCM_OK) return r;
    return fill_counted_buffer(resp.sources_size(), [&](int32_t i) {
        if (out_sources) fill_playlist_source(resp.sources(i), &out_sources[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_SetPlaylistSources(
    OmniPcmClientHandle client,
    const char* instance_id,
    const OmniPcmPlaylistSourceSpec* sources,
    int32_t source_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || source_count < 0 || (source_count > 0 && !sources)) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetPlaylistSourcesRequest req;
    req.set_instance_id(instance_id);
    for (int32_t i = 0; i < source_count; ++i) {
        const auto& src = sources[i];
        auto* out = req.add_sources();
        if (src.id) out->set_id(src.id);
        if (src.name) out->set_name(src.name);
        if (src.ref_id) out->set_ref_id(src.ref_id);
        out->set_kind(static_cast<omni_mix_player::PlaylistSourceKind>(src.kind));
        if (src.uuid_count < 0 || (src.uuid_count > 0 && !src.uuids)) return OMNI_PCM_BAD_ARGUMENT;
        for (int32_t j = 0; j < src.uuid_count; ++j) if (src.uuids[j]) out->add_uuids(src.uuids[j]);
    }
    omni_mix_player::SetPlaylistSourcesResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetPlaylistSources", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_GetEqualizer(
    OmniPcmClientHandle client,
    const char* instance_id,
    OmniPcmEqualizerStateInfo* out_state,
    OmniPcmEqualizerPointInfo* out_points,
    int32_t* inout_point_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !out_state || !inout_point_count) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::GetEqualizerRequest req;
    req.set_instance_id(instance_id);
    omni_mix_player::EqualizerState resp;
    int r = grpc_web_unary(c, "omni_mix_player.PlaybackService", "GetEqualizer", req, &resp);
    if (r != OMNI_PCM_OK) return r;
    out_state->enabled = resp.enabled() ? 1 : 0;
    out_state->global_gain_db = resp.global_gain_db();
    out_state->soft_clip_enabled = resp.soft_clip_enabled() ? 1 : 0;
    return fill_counted_buffer(resp.points_size(), [&](int32_t i) {
        if (out_points) fill_equalizer_point(resp.points(i), &out_points[i]);
    }, inout_point_count);
}

OMNI_PCM_API int OmniPcmClient_SetEqualizer(
    OmniPcmClientHandle client,
    const char* instance_id,
    const OmniPcmEqualizerStateInfo* state,
    const OmniPcmEqualizerPointInfo* points,
    int32_t point_count) {
    auto* c = client_ctx(client);
    if (!c || !instance_id || !*instance_id || !state || point_count < 0 || (point_count > 0 && !points)) return OMNI_PCM_BAD_ARGUMENT;
    omni_mix_player::SetEqualizerRequest req;
    req.set_instance_id(instance_id);
    auto* eq = req.mutable_state();
    eq->set_enabled(state->enabled != 0);
    eq->set_global_gain_db(state->global_gain_db);
    eq->set_soft_clip_enabled(state->soft_clip_enabled != 0);
    for (int32_t i = 0; i < point_count; ++i) {
        auto* out = eq->add_points();
        out->set_id(points[i].id);
        out->set_frequency(points[i].frequency);
        out->set_gain_db(points[i].gain_db);
        out->set_q(points[i].q);
        out->set_type(static_cast<omni_mix_player::EqualizerFilterType>(points[i].type));
    }
    omni_mix_player::SetEqualizerResponse resp;
    return grpc_web_unary(c, "omni_mix_player.PlaybackService", "SetEqualizer", req, &resp);
}

OMNI_PCM_API int OmniPcmClient_GetBackendInfo(OmniPcmClientHandle client, OmniPcmBackendInfo* out_info) {
    auto* c = client_ctx(client);
    if (!c || !out_info) return OMNI_PCM_BAD_ARGUMENT;
    std::memset(out_info, 0, sizeof(*out_info));

    auto http = make_http_client(c);
    auto health = http.Get("/api/health");
    if (!health || health->status < 200 || health->status >= 300) {
        c->set_error("Backend health request failed");
        return OMNI_PCM_ERROR;
    }

    copy_text(out_info->status, sizeof(out_info->status), json_string_value(health->body, "status"));
    out_info->timestamp = json_int64_value(health->body, "timestamp");

    auto version = http.Get("/api/version");
    if (version && version->status >= 200 && version->status < 300) {
        copy_text(out_info->version, sizeof(out_info->version), json_string_value(version->body, "version"));
        copy_text(out_info->name, sizeof(out_info->name), json_string_value(version->body, "name"));
    }
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcmClient_StopBackend(OmniPcmClientHandle client) {
    auto* c = client_ctx(client);
    if (!c) return OMNI_PCM_BAD_ARGUMENT;
    auto http = make_http_client(c);
    auto result = http.Post("/api/backend/stop", "", "application/json");
    if (!result || result->status < 200 || result->status >= 300) {
        c->set_error("Backend stop request failed");
        return OMNI_PCM_ERROR;
    }
    return OMNI_PCM_OK;
}

OMNI_PCM_API int OmniPcmClient_StartEvents(
    OmniPcmClientHandle client,
    OmniPcmEventCallback callback,
    void* user_data) {
    auto* c = client_ctx(client);
    if (!c || !callback) return OMNI_PCM_BAD_ARGUMENT;
    OmniPcmClient_StopEvents(client);
    c->event_stop.store(false);
#ifdef _WIN32
    c->event_thread = std::thread([c, callback, user_data]() {
        websocket_event_loop(c, callback, user_data);
    });
    return OMNI_PCM_OK;
#else
    c->set_error("Event WebSocket client is currently implemented for Windows only");
    return OMNI_PCM_UNSUPPORTED;
#endif
}

/* ── Library queries ──────────────────────────────────────────── */

static void fill_track(const omni_mix_player::Track& src, OmniPcmTrackInfo* dst) {
    if (!dst) return;
    std::memset(dst, 0, sizeof(*dst));
    copy_text(dst->uuid, sizeof(dst->uuid), src.uuid());
    copy_text(dst->title, sizeof(dst->title), src.title());
    copy_text(dst->artist, sizeof(dst->artist), src.artist());
    copy_text(dst->album_id, sizeof(dst->album_id), src.album_id());
    copy_text(dst->module_id, sizeof(dst->module_id), src.module_id());
    copy_text(dst->cover_uri, sizeof(dst->cover_uri), src.cover_uri());
    dst->track_number = 0;
    dst->duration = src.duration();
    dst->is_excluded = src.is_excluded() ? 1 : 0;
    dst->created_at = src.has_created_at() ? src.created_at().seconds() : 0;
    dst->last_played_at = src.has_last_played_at() ? src.last_played_at().seconds() : 0;
}

static void fill_album(const omni_mix_player::Album& src, OmniPcmAlbumInfo* dst) {
    if (!dst) return;
    std::memset(dst, 0, sizeof(*dst));
    copy_text(dst->id, sizeof(dst->id), src.id());
    copy_text(dst->title, sizeof(dst->title), src.title());
    copy_text(dst->artist, sizeof(dst->artist), src.artist());
    copy_text(dst->module_id, sizeof(dst->module_id), src.module_id());
    copy_text(dst->cover_uri, sizeof(dst->cover_uri), src.cover_uri());
    dst->track_count = 0;
}

static void fill_tag(const omni_mix_player::Tag& src, OmniPcmTagInfo* dst) {
    if (!dst) return;
    std::memset(dst, 0, sizeof(*dst));
    copy_text(dst->id, sizeof(dst->id), src.id());
    copy_text(dst->name, sizeof(dst->name), src.name());
    copy_text(dst->module_id, sizeof(dst->module_id), src.module_id());
    copy_text(dst->color, sizeof(dst->color), src.color());
}

static void fill_playlist(const omni_mix_player::Playlist& src, OmniPcmPlaylistInfo* dst) {
    if (!dst) return;
    std::memset(dst, 0, sizeof(*dst));
    copy_text(dst->id, sizeof(dst->id), src.id());
    copy_text(dst->name, sizeof(dst->name), src.name());
    copy_text(dst->module_id, sizeof(dst->module_id), src.module_id());
    copy_text(dst->cover_uri, sizeof(dst->cover_uri), src.cover_uri());
    dst->track_count = 0;
}

OMNI_PCM_API int OmniPcmClient_QueryTracks(
    OmniPcmClientHandle client,
    const OmniPcmTrackQuery* query,
    OmniPcmTrackInfo* out_tracks,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !inout_count) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::TrackQuery req;
    if (query) {
        if (query->album_id) req.set_album_id(query->album_id);
        if (query->tag_id) req.add_tag_ids(query->tag_id);
        if (query->playlist_id) req.set_playlist_id(query->playlist_id);
        if (query->module_id) req.set_module_id(query->module_id);
        if (query->is_excluded >= 0) req.set_is_excluded(query->is_excluded != 0);
        req.set_limit(query->limit > 0 ? query->limit : 0);
        req.set_offset(query->offset);
    }
    omni_mix_player::QueryTracksResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.LibraryService", "QueryTracks", req, &resp);
    if (r != OMNI_PCM_OK) return r;

    return fill_counted_buffer(resp.tracks_size(), [&](int32_t i) {
        if (out_tracks) fill_track(resp.tracks(i), &out_tracks[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_QueryAlbums(
    OmniPcmClientHandle client,
    const OmniPcmLibraryQuery* query,
    OmniPcmAlbumInfo* out_albums,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !inout_count) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::AlbumQuery req;
    if (query) {
        if (query->module_id) req.set_module_id(query->module_id);
        req.set_limit(query->limit > 0 ? query->limit : 0);
        req.set_offset(query->offset);
    }
    omni_mix_player::QueryAlbumsResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.LibraryService", "QueryAlbums", req, &resp);
    if (r != OMNI_PCM_OK) return r;

    return fill_counted_buffer(resp.albums_size(), [&](int32_t i) {
        if (out_albums) fill_album(resp.albums(i), &out_albums[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_QueryTags(
    OmniPcmClientHandle client,
    const OmniPcmLibraryQuery* query,
    OmniPcmTagInfo* out_tags,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !inout_count) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::TagQuery req;
    if (query) {
        if (query->module_id) req.set_module_id(query->module_id);
        req.set_limit(query->limit > 0 ? query->limit : 0);
        req.set_offset(query->offset);
    }
    omni_mix_player::QueryTagsResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.LibraryService", "QueryTags", req, &resp);
    if (r != OMNI_PCM_OK) return r;

    return fill_counted_buffer(resp.tags_size(), [&](int32_t i) {
        if (out_tags) fill_tag(resp.tags(i), &out_tags[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_QueryPlaylists(
    OmniPcmClientHandle client,
    const OmniPcmLibraryQuery* query,
    OmniPcmPlaylistInfo* out_playlists,
    int32_t* inout_count) {
    auto* c = client_ctx(client);
    if (!c || !inout_count) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::PlaylistQuery req;
    if (query) {
        if (query->module_id) req.set_module_id(query->module_id);
        req.set_limit(query->limit > 0 ? query->limit : 0);
        req.set_offset(query->offset);
    }
    omni_mix_player::QueryPlaylistsResponse resp;
    int r = grpc_web_unary(c, "omni_mix_player.LibraryService", "QueryPlaylists", req, &resp);
    if (r != OMNI_PCM_OK) return r;

    return fill_counted_buffer(resp.playlists_size(), [&](int32_t i) {
        if (out_playlists) fill_playlist(resp.playlists(i), &out_playlists[i]);
    }, inout_count);
}

OMNI_PCM_API int OmniPcmClient_GetTrack(
    OmniPcmClientHandle client,
    const char* uuid,
    OmniPcmTrackInfo* out_track) {
    auto* c = client_ctx(client);
    if (!c || !uuid || !out_track) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::GetTrackRequest req;
    req.set_uuid(uuid);
    omni_mix_player::Track resp;
    int r = grpc_web_unary(c, "omni_mix_player.LibraryService", "GetTrack", req, &resp);
    if (r == OMNI_PCM_OK) fill_track(resp, out_track);
    return r;
}

OMNI_PCM_API int OmniPcmClient_SetTrackExcluded(
    OmniPcmClientHandle client,
    const char* uuid,
    int32_t excluded) {
    auto* c = client_ctx(client);
    if (!c || !uuid) return OMNI_PCM_BAD_ARGUMENT;

    omni_mix_player::GetTrackRequest get_req;
    get_req.set_uuid(uuid);
    omni_mix_player::Track track;
    int r = grpc_web_unary(c, "omni_mix_player.LibraryService", "GetTrack", get_req, &track);
    if (r != OMNI_PCM_OK) return r;

    track.set_is_excluded(excluded != 0);

    omni_mix_player::UpsertTrackRequest upsert_req;
    *upsert_req.mutable_track() = std::move(track);
    omni_mix_player::UpsertTrackResponse resp;
    return grpc_web_unary(c, "omni_mix_player.LibraryService", "UpsertTrack", upsert_req, &resp);
}

OMNI_PCM_API void OmniPcmClient_StopEvents(OmniPcmClientHandle client) {
    auto* c = client_ctx(client);
    if (!c) return;
    c->event_stop.store(true);
    if (c->event_thread.joinable()) {
        c->event_thread.join();
    }
}
