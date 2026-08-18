namespace team_hub_chat.Services;

public static class ChatCodes
{
    public static class ConversationTypes
    {
        public const string Direct = "DIRECT";
        public const string Group = "GROUP";
    }

    public static class MemberRoles
    {
        public const string Owner = "OWNER";
        public const string Admin = "ADMIN";
        public const string Member = "MEMBER";
    }

    public static class MessageTypes
    {
        public const string Text = "TEXT";
        public const string System = "SYSTEM";
    }

    public static bool IsConversationType(string value) =>
        value is ConversationTypes.Direct or ConversationTypes.Group;

    public static bool IsMemberRole(string value) =>
        value is MemberRoles.Owner or MemberRoles.Admin or MemberRoles.Member;

    public static bool IsMessageType(string value) =>
        value is MessageTypes.Text or MessageTypes.System;

    public static bool CanManageMembers(string role) =>
        role is MemberRoles.Owner or MemberRoles.Admin;
}
