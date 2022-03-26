using IPA;
using IPALogger = IPA.Logging.Logger;
using HarmonyLib;
using IPA.Config.Stores;
using BeatSaberMarkupLanguage;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using System.Linq;
using System.Reflection;
using TMPro;
using System.Collections.Generic;
namespace InSongLeaderboard
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static List<LeaderboardInfo> storedScores = new List<LeaderboardInfo>();
        public static string currentPlayerName = "";
        public static int currentPlayerScore = 0;
        public static int currentMaxPossibleScore = 0;
        public static int maxPossibleScore = 0;
        internal static Plugin? Instance { get; private set; }
        internal static IPALogger? log { get; set; }

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config config)
        {
            Instance = this;
            log = logger;
            PluginConfig.Instance = config.Generated<PluginConfig>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            var harmony = new Harmony("com.kyle1413.BeatSaber.InSongLeaderboard");
            harmony.PatchAll();
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += BSEvents_GameSceneLoaded;
            BS_Utils.Utilities.BSEvents.lateMenuSceneLoadedFresh += BSEvents_lateMenuSceneLoadedFresh;
            BS_Utils.Utilities.BSEvents.difficultySelected += BSEvents_difficultySelected;
            BS_Utils.Utilities.BSEvents.levelSelected += BSEvents_levelSelected;
            BS_Utils.Utilities.BSEvents.menuSceneLoaded += BSEvents_menuSceneLoaded;
        }

        private void BSEvents_levelSelected(LevelCollectionViewController arg1, IPreviewBeatmapLevel arg2)
        {
           // log.Info("Level selected");
            storedScores.Clear();
        }

        private void BSEvents_menuSceneLoaded()
        {
           
        }

        private async void BSEvents_lateMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            var userInfo = await BS_Utils.Gameplay.GetUserInfo.GetUserAsync();
            currentPlayerName = userInfo.userName;
        }

        private void BSEvents_difficultySelected(StandardLevelDetailViewController arg1, IDifficultyBeatmap arg2)
        {
          //  log.Info("Diff selected");
            storedScores.Clear();
        }

        private InSongBoard SetupLeaderboardObject()
        {
            GameObject Leaderboard = new GameObject("InSongLeaderboard");
            Canvas canvas = Leaderboard.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler cs = Leaderboard.AddComponent<CanvasScaler>();
            cs.scaleFactor = 1.0f;
            cs.dynamicPixelsPerUnit = 10f;
            GraphicRaycaster gr = Leaderboard.AddComponent<GraphicRaycaster>();
            // Leaderboard.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1f);
            // Leaderboard.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);

            GameObject? coreGameHUD = Resources.FindObjectsOfTypeAll<CoreGameHUDController>()?.FirstOrDefault(x => x.isActiveAndEnabled)?.gameObject ?? null;
            FlyingGameHUDRotation flyingGameHUD = Resources.FindObjectsOfTypeAll<FlyingGameHUDRotation>().FirstOrDefault(x => x.isActiveAndEnabled);
            if (coreGameHUD != null)
                Leaderboard.transform.SetParent(coreGameHUD.transform, true);
            //      textObj.transform.position = new Vector3(0, 0f, 0);
            float depth = coreGameHUD != null ? coreGameHUD.transform.GetChild(1).transform.position.z : 9f;
            if (flyingGameHUD != null)
            {
                depth = flyingGameHUD.transform.GetChild(0).transform.position.z / 2;
              //  Leaderboard.transform.localPosition = new Vector3(0, 0.75f, 6f);
                Leaderboard.transform.eulerAngles = new Vector3(345f, 0f, 0f);
            }
            Leaderboard.transform.localPosition = new Vector3(PluginConfig.Instance.position.x, PluginConfig.Instance.position.y, depth);
            Leaderboard.transform.localRotation = Quaternion.identity;
            Leaderboard.transform.localScale = PluginConfig.Instance.scale * new Vector3(0.06f, 0.06f, 0.06f);
            var canvasSettings = Leaderboard.AddComponent<CurvedCanvasSettings>();
            canvasSettings.SetRadius(0);

            var boardHandler = Leaderboard.AddComponent<InSongBoard>();

            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.
                GetResourceContent(Assembly.GetExecutingAssembly(), "InSongLeaderboard.board.bsml"), Leaderboard, boardHandler);
            return boardHandler;
        }
        private void BSEvents_GameSceneLoaded()
        {
            if (!BS_Utils.Plugin.LevelData.IsSet || BS_Utils.Plugin.LevelData.Mode != BS_Utils.Gameplay.Mode.Standard || !PluginConfig.Instance.enabled)
                return;
            var scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().LastOrDefault();
            if (scoreController == null) return;
            
            //Reset score values
            currentPlayerScore = 0;
            currentMaxPossibleScore = 0;
            maxPossibleScore = scoreController.immediateMaxPossibleModifiedScore;
            //Create board
            var boardHandler = SetupLeaderboardObject();
            //Setup events
            scoreController.scoreDidChangeEvent += ScoreController_scoreDidChangeEvent;
            scoreController.scoreDidChangeEvent += delegate(int score,int modifiedScore) { ScoreController_scoreDidChangeEvent(score, modifiedScore, boardHandler); };

        }

        private void ScoreController_scoreDidChangeEvent(int score, int modifiedScore, InSongBoard leaderboard)
        {
          //  log.Debug("Score Update: " + score);

            currentPlayerScore = score;
            storedScores.RemoveAll(x => x.playerPosition == 0);
            storedScores.Add(new LeaderboardInfo(currentPlayerName, currentPlayerScore, 0));
        //    leaderboard.UpdateScores();
        }

        private void ScoreController_scoreDidChangeEvent(int arg1, int arg2)
        {
          //  log.Debug("Max Score Update: " + arg1);
            currentMaxPossibleScore = arg1;
        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }



        public static void GrabScores()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameCore") return;

            var boards = Resources.FindObjectsOfTypeAll<PlatformLeaderboardViewController>().First()?.GetComponentInChildren<LeaderboardTableView>()?.gameObject?
                .transform?.Find("Viewport")?.Find("Content")?.GetComponentsInChildren<LeaderboardTableCell>();
            if (boards != null)
            {
                try
                {
                    foreach (LeaderboardTableCell cell in boards)
                    {
                        var cellTexts = cell.GetComponentsInChildren<TextMeshProUGUI>();
                        string playerName = "";
                        int pos = -1;
                        int score = -1;
                        foreach (TextMeshProUGUI text in cellTexts)
                        {
                            if (text.name == "PlayerName")
                            {
                                playerName = text.text;

                                if (PluginConfig.Instance.simpleNames)
                                {
                                    if (text.text.Contains("<size=85%>"))
                                    {
                                        Plugin.log!.Info("1 " + playerName);
                                        var splitText = text.text.Split('>', '<');
                                        playerName = splitText[2];
                                        if (string.IsNullOrWhiteSpace(playerName) && splitText.Length >= 5)
                                            playerName = splitText[4];
                                        if (!string.IsNullOrWhiteSpace(playerName) && playerName.Contains(" - "))
                                            playerName = playerName.Substring(0, playerName.Length - 2);
                                        //playerName = playerName.Remove(Mathf.Clamp(playerName.Length - 3, 0, playerName.Length), 3);

                                    }
                                    else if (text.text.Contains("<size=75%>"))
                                    {
                                        playerName = text.text.Split('<')[0];
                                        //  Plugin.log.Info("2 " + playerName);
                                        if (!string.IsNullOrWhiteSpace(playerName) && playerName.Contains(" - "))
                                            playerName = playerName.Substring(0, playerName.Length - 2);
                                        //    playerName = playerName.Substring(0, playerName.LastIndexOf('-'));
                                        // playerName = playerName.Remove(Mathf.Clamp(playerName.Length - 3, 0, playerName.Length), 3);
                                    }

                                }

                            }
                            if (text.name == "Rank")
                            {
                                pos = int.Parse(text.text);
                            }
                            if (text.name == "Score")
                            {
                                score = int.Parse(text.text.Replace(" ", ""));
                            }

                        }
                        // log.Info($"Processed Score: {playerName} | {score} | {pos}");
                        LeaderboardInfo entry = new LeaderboardInfo(playerName, score, pos);
                        if (!storedScores.Any(x => (x.playerName == entry.playerName && x.playerScore == entry.playerScore)))
                            storedScores.Add(entry);
                       //      else
                       //        Plugin.log.Info("Entry already present");

                    }
                }
                catch(System.Exception ex)
                {
                    Plugin.log!.Error($"Failed to grab scores from Leaderboard {ex}");
                }
            }
             

            //foreach (LeaderboardInfo entry in playerScores)
            //  {
            //      Log("Yoinking Leaderboard Entry for Position: " + entry.playerPosition);
            //      Log("Name: " + entry.playerName);
            //      Log("Score: " + entry.playerScore);
            //}
        }
    }
}
