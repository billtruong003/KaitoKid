namespace SpumOnline
{
    /// <summary>
    /// Hang so trang thai animation dung chung giua client va server.
    /// Khop voi CharacterVisualSync.AnimStateMap va MobController.AnimStateMap.
    /// </summary>
    public static class AnimState
    {
        public const int Idle = 0;
        public const int Move = 1;
        public const int Attack = 2;
        public const int Death = 3;
    }
}
