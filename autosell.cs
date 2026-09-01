//MCCScript 1.0

MCC.LoadBot(new AutoKickBot());

//MCCScript Extensions

public class AutoKickBot : ChatBot
{
    private bool isRunning = true;
    private bool needsLogin = true;
    private bool isReady = false;

    // =========================================================================
    // YOUR PASSWORD
    // =========================================================================
    private string myPassword = "XXX0XXX"; 

    // =========================================================================
    // TARGET PLAYERS TO KICK
    // =========================================================================
    private readonly System.Collections.Generic.HashSet<string> targetPlayers = 
        new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "_FeedMyEgo",
        "zombiefff",
        "Artlais",
        "DN_Dibbo",
        "fartlord__",
        "MainVictim",
        "TrustRat",
        "ZoroRx",
        "Relayy4"
    };

    // Cooldown dictionary to prevent kick command spamming in the same second
    private readonly System.Collections.Generic.Dictionary<string, System.DateTime> lastKicked = 
        new System.Collections.Generic.Dictionary<string, System.DateTime>(System.StringComparer.OrdinalIgnoreCase);

    public override void Initialize()
    {
        isRunning = true;
        needsLogin = true;
        isReady = false;
        lastKicked.Clear();

        LogToConsole("==================================================");
        LogToConsole("      AUTO-KICK MODERATION BOT LOADED            ");
        LogToConsole("==================================================");

        System.Threading.Thread botThread = new System.Threading.Thread(() => RunBotSequence());
        botThread.IsBackground = true;
        botThread.Start();
    }

    public override void GetText(string text, string json)
    {
        text = GetVerbatim(text);
        if (text.Contains("You are already logged!") || text.Contains("Successfully logged in"))
        {
            needsLogin = false;
        }
    }

    // Continuously scans the tab-list for target players
    public override void Update()
    {
        if (!isReady || !isRunning) return;

        try
        {
            string[] onlinePlayers = GetOnlinePlayers();
            if (onlinePlayers != null)
            {
                foreach (string player in onlinePlayers)
                {
                    CheckAndKickPlayer(player);
                }
            }
        }
        catch { }
    }

    private void CheckAndKickPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;

        string cleanName = playerName.Trim();

        if (targetPlayers.Contains(cleanName))
        {
            // Only send the kick command if we haven't tried kicking them in the last 10 seconds
            if (lastKicked.TryGetValue(cleanName, out System.DateTime lastTime))
            {
                if ((System.DateTime.Now - lastTime).TotalSeconds < 10)
                {
                    return;
                }
            }

            lastKicked[cleanName] = System.DateTime.Now;

            LogToConsole("==================================================");
            LogToConsole("[TARGET DETECTED] " + cleanName + " found on server!");
            LogToConsole("[ACTION] Executing kick command...");
            LogToConsole("==================================================");

            SafeSendText("/kick " + cleanName + " &a&c&l Internal error occurred");
        }
    }

    public override bool OnDisconnect(DisconnectReason reason, string message)
    {
        isRunning = false;
        isReady = false;
        LogToConsole("Disconnected from server (" + reason + ").");
        UnloadBot();
        return false;
    }

    private void SafeSendText(string text)
    {
        if (!isRunning) return;
        try { SendText(text); } catch { }
    }

    private void SafePerformInternalCommand(string cmd)
    {
        if (!isRunning) return;
        try { PerformInternalCommand(cmd); } catch { }
    }

    private bool IsAtLifesteal(Location loc)
    {
        // Check if at X: 5, Z: 273 (within a 10-block radius)
        bool atSpawn = System.Math.Abs(loc.X - 5) <= 10 && 
                       System.Math.Abs(loc.Z - 273) <= 10;

        // Check if at base coordinates
        bool atBase = System.Math.Abs(loc.X - 7223) <= 5 && 
                      System.Math.Abs(loc.Y - (-18)) <= 3 && 
                      System.Math.Abs(loc.Z - (-4914)) <= 5;

        return atSpawn || atBase;
    }

    private bool EnsureAtLifesteal()
    {
        while (isRunning)
        {
            var loc = GetCurrentLocation();
            int roundedX = (int)System.Math.Round(loc.X);
            int roundedY = (int)System.Math.Round(loc.Y);
            int roundedZ = (int)System.Math.Round(loc.Z);

            LogToConsole("[Location Check] Current Pos: (" + roundedX + ", " + roundedY + ", " + roundedZ + ")");

            if (IsAtLifesteal(loc))
            {
                LogToConsole("--> Confirmed at Lifesteal.");
                return true;
            }

            LogToConsole("--> Not at Lifesteal base.");

            if (needsLogin)
            {
                LogToConsole("--> Waiting 4 seconds, then sending login...");
                System.Threading.Thread.Sleep(4000);
                SafeSendText("/login " + myPassword);
            }
            else
            {
                LogToConsole("--> Already authenticated.");
            }

            LogToConsole("--> Waiting 5 seconds before switching servers...");
            System.Threading.Thread.Sleep(5000);

            LogToConsole("--> Anchoring yaw/pitch to 0, 0...");
            SafePerformInternalCommand("look 0 0");
            System.Threading.Thread.Sleep(1000);

            LogToConsole("--> Sending '/server lifesteal'...");
            SafeSendText("/server lifesteal");

            LogToConsole("--> Freezing bot for 15 seconds to let transfer complete...");
            System.Threading.Thread.Sleep(15000);
        }

        return false;
    }

    private void RunBotSequence()
    {
        System.Threading.Thread.Sleep(3000);

        while (isRunning)
        {
            try
            {
                if (!EnsureAtLifesteal())
                {
                    break;
                }

                LogToConsole("--> Monitoring active: Bot is now actively checking for target players.");
                isReady = true;

                // Keep the thread alive with Anti-AFK sneaking while monitoring
                while (isRunning && IsAtLifesteal(GetCurrentLocation()))
                {
                    for (int second = 0; second < 60 && isRunning; second++)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    if (isRunning)
                    {
                        SafePerformInternalCommand("sneak");
                        System.Threading.Thread.Sleep(300);
                        SafePerformInternalCommand("sneak");
                    }
                }

                isReady = false;
            }
            catch (System.Exception ex)
            {
                LogToConsole("SCRIPT ERROR: " + ex.Message + " | Retrying in 5 seconds...");
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
