using UnityEngine;

namespace CatTanks.Editor
{
    public static class ArtAndLayoutValidation
    {
        public static void Run()
        {
            if(!Application.isBatchMode)throw new System.InvalidOperationException("Run in the dedicated test editor.");
            MonsterAnimationImporter.ImportLatest();
            MonsterAnimationValidation.Validate();
            ProjectSetup.ValidatePort();
            MockupGameUIValidation.BuildAndTest();
        }
    }
}
