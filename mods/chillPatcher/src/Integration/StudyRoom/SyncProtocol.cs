using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// 同步消息类型枚举
    /// </summary>
    public enum SyncMessageType : byte
    {
        // === 握手 (1-9) ===
        JoinRequest       = 1,
        Challenge         = 2,
        ChallengeResponse = 3,
        JoinAccepted      = 4,
        JoinRejected      = 5,
        SyncReady         = 6,
        ReconnectRequest  = 7,

        // === 状态快照 (10-19) ===
        FullSnapshot      = 10,

        // === 角色同步 (20-29) ===
        StateChanged      = 20,
        ScenarioPlay      = 21,
        VoicePlay         = 22,
        VoiceCancel       = 23,
        SubtitleShow      = 24,
        SubtitleHide      = 25,

        // === 番茄钟 (30-39) ===
        PomodoroSnapshot  = 30,
        PomodoroEvent     = 31,

        // === 进度 (40-49) ===
        ExpChanged        = 40,
        LevelChanged      = 41,
        WorkSecondsSync   = 42,

        // === 环境/装饰/模式 (50-59) ===
        EnvironmentViewChange  = 50,
        EnvironmentSoundChange = 51,
        EnvironmentPresetLoad  = 52,
        EnvironmentAutoTime    = 53,
        DecorationChange       = 54,
        DecorationPresetLoad   = 55,
        ModeChange             = 56,
        PointPurchaseSync      = 57,

        // === 互动 (60-69) ===
        InteractionReq    = 60,
        InteractionGrant  = 61,
        InteractionDeny   = 62,
        InteractionEnd    = 63,

        // === 故事 (70-79) ===
        StoryReady        = 70,
        StoryAck          = 71,
        StoryStart        = 72,
        StorySkip         = 73,

        // === 存档协同 (80-89) ===
        SaveDataOp        = 80,
        SaveDataChanged   = 81,
        SaveDataFull      = 82,
        EditingStart      = 83,
        EditingEnd        = 84,

        // === 预留: 语音聊天 (100-109) ===
        VoiceChatData     = 100,
        VoiceChatMute     = 101,
        VoiceChatState    = 102,

        // === 预留: 音乐广播 (110-119) ===
        MusicSync         = 110,
        MusicStreamData   = 111,
        MusicTimeSync     = 112,
        MusicInfo         = 113,

        // === 预留: 聊天 (120-129) ===
        ChatMessage       = 120,
        ChatEmote         = 121,

        // === 控制 (200-209) ===
        Heartbeat         = 200,
        Kick              = 201,
        RoomClosed        = 202,
        PlayerJoined      = 203,
        PlayerLeft        = 204,

        // === 序列号包装 ===
        SeqWrapper        = 250,
    }

    /// <summary>
    /// 存档操作子类型
    /// </summary>
    public enum SaveDataOpType : byte
    {
        // 待办事项 (1-19)
        TodoListCreate    = 1,
        TodoListDelete    = 2,
        TodoListRename    = 3,
        TodoItemAdd       = 10,
        TodoItemDelete    = 11,
        TodoItemUpdate    = 12,
        TodoItemReorder   = 13,

        // 日历/日记 (20-29)
        CalendarDiaryEdit = 20,
        CalendarWorkTime  = 21,

        // 备忘录 (30-39)
        NotePageCreate    = 30,
        NotePageDelete    = 31,
        NotePageUpdate    = 32,
        NotePageRename    = 33,
        NotePageReorder   = 34,

        // 习惯追踪 (40-49)
        HabitCreate       = 40,
        HabitDelete       = 41,
        HabitUpdate       = 42,
        HabitToggleDay    = 43,
        HabitHideOnCal    = 44,
        HabitPause        = 45,
        HabitResume       = 46,
    }

    /// <summary>
    /// 同步消息结构体
    /// 格式: [1 byte: MessageType] [2 bytes: PayloadLength] [N bytes: JSON Payload]
    /// </summary>
    public struct SyncMessage
    {
        public SyncMessageType Type;
        public JObject Payload;

        public SyncMessage(SyncMessageType type, JObject payload = null)
        {
            Type = type;
            Payload = payload ?? new JObject();
        }
    }

    /// <summary>
    /// 消息序列化/反序列化 + 可靠性分层判断
    /// </summary>
    public static class SyncProtocol
    {
        private static readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();

        /// <summary>
        /// 将 SyncMessage 序列化为 byte[]
        /// 格式: [1 byte: Type] [2 bytes: PayloadLength LE] [N bytes: JSON UTF8]
        /// </summary>
        public static byte[] Serialize(SyncMessage msg)
        {
            var json = msg.Payload?.ToString(Formatting.None) ?? "{}";
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var length = (uint)jsonBytes.Length;

            var result = new byte[5 + jsonBytes.Length];
            result[0] = (byte)msg.Type;
            result[1] = (byte)(length & 0xFF);
            result[2] = (byte)((length >> 8) & 0xFF);
            result[3] = (byte)((length >> 16) & 0xFF);
            result[4] = (byte)((length >> 24) & 0xFF);
            Array.Copy(jsonBytes, 0, result, 5, jsonBytes.Length);
            return result;
        }

        /// <summary>
        /// 将 byte[] 反序列化为 SyncMessage
        /// </summary>
        public static bool TryDeserialize(byte[] data, int offset, int count, out SyncMessage msg)
        {
            msg = default;
            if (count < 5) return false;

            var type = (SyncMessageType)data[offset];
            var payloadLen = (uint)(data[offset + 1]
                | (data[offset + 2] << 8)
                | (data[offset + 3] << 16)
                | (data[offset + 4] << 24));

            if (count < 5 + (int)payloadLen) return false;

            JObject payload;
            if (payloadLen == 0)
            {
                payload = new JObject();
            }
            else
            {
                var json = Encoding.UTF8.GetString(data, offset + 5, (int)payloadLen);
                try { payload = JObject.Parse(json); }
                catch { return false; }
            }

            msg = new SyncMessage(type, payload);
            return true;
        }

        /// <summary>
        /// 创建携带 payload 字典的消息
        /// </summary>
        public static SyncMessage Create(SyncMessageType type, Dictionary<string, object> data = null)
        {
            var payload = data != null ? JObject.FromObject(data) : new JObject();
            return new SyncMessage(type, payload);
        }

        /// <summary>
        /// 判断该消息类型是否需要可靠传输
        /// </summary>
        public static bool IsReliable(SyncMessageType type)
        {
            switch (type)
            {
                // 不可靠: 番茄钟快照、心跳
                case SyncMessageType.PomodoroSnapshot:
                case SyncMessageType.Heartbeat:
                case SyncMessageType.VoiceChatData:
                case SyncMessageType.MusicStreamData:
                case SyncMessageType.MusicTimeSync:
                    return false;
                // 其余全部可靠
                default:
                    return true;
            }
        }

        /// <summary>
        /// 判断该消息类型是否需要序列号包装 (用于断点续传)
        /// 仅可靠的状态变更消息需要,握手/控制/快照不需要
        /// </summary>
        public static bool NeedsSequenceNumber(SyncMessageType type)
        {
            switch (type)
            {
                // 需要序列号: 所有会改变客户端状态的增量消息
                case SyncMessageType.StateChanged:
                case SyncMessageType.ScenarioPlay:
                case SyncMessageType.VoicePlay:
                case SyncMessageType.VoiceCancel:
                case SyncMessageType.SubtitleShow:
                case SyncMessageType.SubtitleHide:
                case SyncMessageType.PomodoroEvent:
                case SyncMessageType.EnvironmentViewChange:
                case SyncMessageType.EnvironmentSoundChange:
                case SyncMessageType.EnvironmentPresetLoad:
                case SyncMessageType.EnvironmentAutoTime:
                case SyncMessageType.DecorationChange:
                case SyncMessageType.DecorationPresetLoad:
                case SyncMessageType.ModeChange:
                case SyncMessageType.PointPurchaseSync:
                case SyncMessageType.InteractionGrant:
                case SyncMessageType.InteractionDeny:
                case SyncMessageType.InteractionEnd:
                case SyncMessageType.StoryReady:
                case SyncMessageType.StoryStart:
                case SyncMessageType.StorySkip:
                case SyncMessageType.SaveDataChanged:
                case SyncMessageType.ExpChanged:
                case SyncMessageType.LevelChanged:
                case SyncMessageType.WorkSecondsSync:
                case SyncMessageType.PlayerJoined:
                case SyncMessageType.PlayerLeft:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 用 SeqWrapper 包装消息 (主机端广播时使用)
        /// </summary>
        public static SyncMessage WrapWithSequence(SyncMessage inner, uint seq)
        {
            var payload = new JObject
            {
                ["seq"] = seq,
                ["type"] = (byte)inner.Type,
                ["payload"] = inner.Payload
            };
            return new SyncMessage(SyncMessageType.SeqWrapper, payload);
        }

        /// <summary>
        /// 解包 SeqWrapper，返回内部消息和序列号
        /// </summary>
        public static bool TryUnwrapSequence(JObject wrapperPayload, out uint seq, out SyncMessage inner)
        {
            seq = 0;
            inner = default;

            if (wrapperPayload == null) return false;
            seq = wrapperPayload.Value<uint>("seq");
            var innerType = (SyncMessageType)wrapperPayload.Value<byte>("type");
            var innerPayload = wrapperPayload["payload"] as JObject ?? new JObject();
            inner = new SyncMessage(innerType, innerPayload);
            return true;
        }
    }
}
