namespace ChillPatcher.Integration.StudyRoom
{
    /// <summary>
    /// 自习室系统配置常量
    /// </summary>
    public static class StudyRoomConfig
    {
        // ─── 心跳 & 断线 ───
        public const float HeartbeatIntervalSeconds = 1.0f;
        public const float DisconnectTimeoutSeconds = 3.0f;
        public const float ReconnectTimeoutSeconds = 60.0f;

        // ─── 番茄钟快照 ───
        public const float PomodoroSnapshotIntervalSeconds = 1.0f;

        // ─── 互动锁 ───
        public const float InteractionLockTimeoutSeconds = 30.0f;

        // ─── 故事等待 ───
        public const float StoryAckTimeoutSeconds = 5.0f;

        // ─── 房间 ───
        public const int MaxMembers = 8;
        public const string LobbyFilterKey = "chill_studyroom";
        public const string ProtocolVersion = "1";

        // ─── P2P ───
        public const int P2PChannel = 42;
        public const int MaxMessagesPerPoll = 64;
        public const int MaxMessageSize = 512 * 1024; // 512KB per Steam P2P limit

        // ─── 子存档 ───
        public const string HostProfilePrefix = "_sr_host_";
        public const string ClientProfilePrefix = "_sr_client_";

        // ─── Lobby 元数据 Key ───
        public const string MetaKey_Filter = "chill_studyroom";
        public const string MetaKey_RoomName = "room_name";
        public const string MetaKey_HostName = "host_name";
        public const string MetaKey_PasswordRequired = "password_required";
        public const string MetaKey_ProtocolVersion = "protocol_version";
        public const string MetaKey_MemberCount = "member_count";
        public const string MetaKey_MaxMembers = "max_members";
        public const string MetaKey_PomodoroState = "pomodoro_state";
        public const string MetaKey_Mode = "mode";

        // ─── 客户端加入时需要隐藏的 UI 元素 (场景树路径) ───
        public static readonly string[] HiddenUIElements = new string[]
        {
            // 在此添加需要隐藏的 UI 树路径，例:
            // "Canvas/PomodoroSettingsPanel",
            // "Canvas/EnvironmentPanel",
        };
    }
}
