//MCCScript 1.0

MCC.LoadBot(new AutoSellBot());

//MCCScript Extensions

public class AutoSellBot : ChatBot
{
    private bool isRunning = true;
    private bool isEvading = false; 
    private bool needsLogin = true; // Tracks if we need to send the password

    // =========================================================================
    // YOUR PASSWORD
    // =========================================================================
    private string myPassword = "XXX0XXX"; 

    // =========================================================================
    // AVOID SPECIFIC PLAYERS (Tab List)
    // =========================================================================
    private string[] avoidPlayers = new string[] 
    { 
        "RanaWise", 
        "_FeedMyEgo" 
    };

    public override void Initialize()
    {
        isRunning = true;
        isEvading = false;
        needsLogin = true; // Reset login state on startup
        LogToConsole("==================================================");
        LogToConsole(" AUTO-SELL + AVOIDPLAYER + ANTI-AFK LOADED ");
        LogToConsole("==================================================");
        
        System.Threading.Thread botThread = new System.Threading.Thread(() => RunFullBotSequence());
        botThread.IsBackground = true; 
        botThread.Start();
    }

    private void EvadeAndReconnect(string warningMessage)
    {
        if (!isRunning) return; 
        isRunning = false;      
        isEvading = true; 
        
        LogToConsole(" ");
        LogToConsole("==================================================");
        LogToConsole(warningMessage);
        LogToConsole("[Evade] Disconnecting from server safely...");
        LogToConsole("[Evade] Will automatically reconnect in 30 minutes.");
        LogToConsole("==================================================");
        
        PerformInternalCommand("disconnect");

        System.Threading.Thread evadeThread = new System.Threading.Thread(() => 
        {
            System.Threading.Thread.Sleep(1800 * 1000); 
            LogToConsole("[Evade] 30 minutes have passed. Reconnecting...");
            
            PerformInternalCommand("reco");
            System.Threading.Thread.Sleep(5000);
            UnloadBot(); 
        });
        evadeThread.IsBackground = true;
        evadeThread.Start();
    }

    // Read chat to avoid sending /login if the server auto-authenticates us via IP
    public override void GetText(string text, string json)
    {
        text = GetVerbatim(text);
        if (text.Contains("You are already logged!") || text.Contains("Successfully logged in"))
        {
            needsLogin = false;
        }
    }

    public override void Update()
    {
        if (!isRunning) return;

        try
        {
            string[] onlinePlayers = GetOnlinePlayers();
            if (onlinePlayers != null)
            {
                foreach (string player in onlinePlayers)
                {
                    foreach (string enemy in avoidPlayers)
                    {
                        if (player.Equals(enemy, System.StringComparison.OrdinalIgnoreCase))
                        {
                            EvadeAndReconnect("[AvoidPlayer] DANGER! " + player + " joined the server.");
                            return;
                        }
                    }
                }
            }

            var loc = GetCurrentLocation();
            if (IsAtLifesteal(loc))
            {
                var entities = GetEntities();
                if (entities != null)
                {
                    foreach (var entity in entities.Values)
                    {
                        if (entity.Type.ToString().ToLower().Contains("player"))
                        {
                            double dist = loc.Distance(entity.Location);
                            if (dist > 0.5 && dist < 150) 
                            {
                                EvadeAndReconnect("[RenderLeave] DANGER! Player detected " + (int)dist + " blocks away.");
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch { } 
    }

    public override void OnEntitySpawn(Entity entity)
    {
        CheckRenderDistancePlayer(entity);
    }

    public override void OnEntityMove(Entity entity)
    {
        CheckRenderDistancePlayer(entity);
    }

    private void CheckRenderDistancePlayer(Entity entity)
    {
        if (!isRunning) return;

        var loc = GetCurrentLocation();
        if (!IsAtLifesteal(loc)) return;

        if (entity != null && entity.Type.ToString().ToLower().Contains("player"))
        {
            double dist = loc.Distance(entity.Location);
            if (dist > 0.5 && dist < 150) 
            {
                EvadeAndReconnect("[RenderLeave] DANGER! Player detected " + (int)dist + " blocks away.");
            }
        }
    }

    public override bool OnDisconnect(DisconnectReason reason, string message)
    {
        isRunning = false;
        LogToConsole("Server disconnected (" + reason + "). Bot thread safely stopped.");
        
        if (isEvading) 
        {
            return true; 
        }
        
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
        return System.Math.Abs(loc.X - 7223) <= 5 && 
               System.Math.Abs(loc.Y - (-18)) <= 3 && 
               System.Math.Abs(loc.Z - (-4914)) <= 5;
    }

    private bool EnsureAtLifesteal()
    {
        while (isRunning)
        {
            var loc = GetCurrentLocation();
            
            if (IsAtLifesteal(loc))
            {
                LogToConsole("--> Confirmed at Lifesteal base. Waiting 8 seconds before starting route...");
                System.Threading.Thread.Sleep(8000);
                return true;
            }

            LogToConsole("--> Not at Lifesteal. We are in the Auth/Fallback area.");

            if (needsLogin)
            {
                LogToConsole("--> Waiting 4 seconds, then sending login...");
                System.Threading.Thread.Sleep(4000);
                SafeSendText("/login " + myPassword);
            }
            else
            {
                LogToConsole("--> Skipped /login (Already authenticated).");
            }
            
            LogToConsole("--> Waiting 6 seconds for server to settle...");
            System.Threading.Thread.Sleep(6000); 

            // =================================================================
            // CAMERA ANCHOR FIX FOR PROXY CRASH
            // =================================================================
            LogToConsole("--> Anchoring yaw/pitch to 0, 0 to stabilize proxy transfer...");
            SafePerformInternalCommand("look 0 0");
            System.Threading.Thread.Sleep(2000); // 2 second delay to let the server register the look packet
            
            LogToConsole("--> Requesting server switch (/server lifesteal)...");
            SafeSendText("/server lifesteal");
            
            LogToConsole("--> Waiting 15 seconds for teleport to process...");
            System.Threading.Thread.Sleep(15000); 
        }

        return false;
    }

    private void RunFullBotSequence()
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

                LogToConsole("Starting Spawner Route...");

                ProcessDirectSpawner(7225, -18, -4911); 
                ProcessDirectSpawner(7224, -18, -4911); 
                ProcessDirectSpawner(7223, -18, -4911); 
                ProcessDirectSpawner(7222, -18, -4911); 
                ProcessDirectSpawner(7225, -16, -4911); 
                ProcessDirectSpawner(7224, -16, -4911); 
                ProcessDirectSpawner(7223, -16, -4911); 
                ProcessDirectSpawner(7222, -16, -4911); 
                ProcessDirectSpawner(7222, -18, -4918); 
                ProcessDirectSpawner(7223, -18, -4918); 
                ProcessDirectSpawner(7224, -18, -4918); 
                ProcessDirectSpawner(7225, -18, -4918); 
                ProcessDirectSpawner(7225, -16, -4918);
                ProcessDirectSpawner(7224, -16, -4918);
                ProcessDirectSpawner(7223, -16, -4918);

                LogToConsole("Route finished successfully. Sleeping for 1 Hour (with Anti-AFK)...");
                
                for (int minute = 0; minute < 30 && isRunning; minute++)
                {
                    for (int second = 0; second < 60 && isRunning; second++)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    if (isRunning && IsAtLifesteal(GetCurrentLocation()))
                    {
                        SafePerformInternalCommand("sneak");
                        System.Threading.Thread.Sleep(300);
                        SafePerformInternalCommand("sneak"); 
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogToConsole("CRITICAL SCRIPT ERROR: " + ex.Message + " | Restarting loop in 10 seconds...");
                System.Threading.Thread.Sleep(10000);
            }
        }
    }

    private System.Collections.Generic.Dictionary<int, string> GetWindowSnapshot()
    {
        var snap = new System.Collections.Generic.Dictionary<int, string>();
        var invs = GetInventories();
        if (invs != null)
        {
            foreach (int id in invs.Keys)
            {
                if (id != 0) snap[id] = invs[id].Title ?? "";
            }
        }
        return snap;
    }

    private int WaitForNewWindow(System.Collections.Generic.Dictionary<int, string> oldSnap, int timeoutMs = 4000)
    {
        int checks = timeoutMs / 200;
        for (int i = 0; i < checks && isRunning; i++)
        {
            System.Threading.Thread.Sleep(200);
            var invs = GetInventories();
            if (invs != null)
            {
                foreach (int id in invs.Keys)
                {
                    if (id == 0) continue; 
                    string currentTitle = invs[id].Title ?? "";
                    if (!oldSnap.ContainsKey(id) || oldSnap[id] != currentTitle)
                    {
                        if (invs[id].Items.Count > 0) return id;
                    }
                }
            }
        }
        return -1;
    }

    private void ProcessDirectSpawner(int x, int y, int z)
    {
        if (!isRunning) return;
        LogToConsole("Direct Selling Spawner at " + x + " " + y + " " + z);
        
        SafePerformInternalCommand("look " + x + " " + y + " " + z);
        System.Threading.Thread.Sleep(1000);

        var snap1 = GetWindowSnapshot();
        SafePerformInternalCommand("useblock " + x + " " + y + " " + z);
        int spawnerWinId = WaitForNewWindow(snap1, 4000);
        System.Threading.Thread.Sleep(1500); 

        var snap2 = GetWindowSnapshot();
        if (spawnerWinId != -1)
            SafePerformInternalCommand("inventory " + spawnerWinId + " click 13");
        else
            SafePerformInternalCommand("inventory container click 13");

        int confirmWinId = WaitForNewWindow(snap2, 4000);
        System.Threading.Thread.Sleep(1500); 

        var snap3 = GetWindowSnapshot();
        if (confirmWinId != -1)
            SafePerformInternalCommand("inventory " + confirmWinId + " click 16");
        else
            SafePerformInternalCommand("inventory container click 16");

        int returnWinId = WaitForNewWindow(snap3, 4000);
        System.Threading.Thread.Sleep(1500); 
        
        if (returnWinId != -1)
            SafePerformInternalCommand("inventory " + returnWinId + " close");
        else
            SafePerformInternalCommand("inventory container close");

        System.Threading.Thread.Sleep(1000);
    }
}
