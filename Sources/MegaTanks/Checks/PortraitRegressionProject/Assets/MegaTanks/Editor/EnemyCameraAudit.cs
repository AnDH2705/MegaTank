namespace MegaTanks.Editor
{
    // Historical validation entry point now checks the single draggable battlefield camera.
    public static class EnemyCameraAudit
    {
        public static void Run() { CameraPanAudit.Run(); }
    }
}
