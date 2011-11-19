#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Timers;
using System.Web.Services.Description;
using System.Xml.Serialization;
using LobbyClient;
using NightWatch;
using ZkData;

#endregion

namespace CaTracker
{
    public class Nightwatch
    {
        DateTime lastStatsSave;

        Timer recon;
        readonly Timer statsTimer = new Timer(60000);
        TasClient tas;

        public Dictionary<string, int> MapPlayerMinutes = new Dictionary<string, int>();
        public Dictionary<string, int> ModPlayerMinutes = new Dictionary<string, int>();

        public TasClient Tas { get { return tas; } }
        public static Config config;
        ServiceHost host;
        OfflineMessages offlineMessages;
        string webRoot;
        PlayerMover playerMover;

        public AuthService Auth { get; private set; }

        public List<Battle> GetPlanetWarsBattles() {
            return tas.ExistingBattles.Values.Where(x => x.Founder.Name.StartsWith("PlanetWars")).ToList();
        }

        public List<Battle> GetPlanetBattles(Planet planet) {
            return GetPlanetWarsBattles().Where(x => x.MapName == planet.Resource.InternalName).ToList();
        }

        public Nightwatch(string webRoot)
		
        {
            this.webRoot = webRoot;
			config = new Config();
			statsTimer.Elapsed += statsTimer_Elapsed;
			statsTimer.AutoReset = true;
			statsTimer.Start();
			
		}


		public bool Start()
		{
			if (config.AttemptToRecconnect)
			{
				recon = new Timer(config.AttemptReconnectInterval*1000);
				recon.AutoReset = true;
				recon.Elapsed += recon_Elapsed;
			}

			recon.Enabled = false;

			tas = new TasClient(null, "NightWatch");
			tas.ConnectionLost += tas_ConnectionLost;
			tas.Connected += tas_Connected;
			tas.LoginDenied += tas_LoginDenied;
			tas.LoginAccepted += tas_LoginAccepted;

            

			try
			{
				tas.Connect(config.ServerHost, config.ServerPort);
			}
			catch
			{
				recon.Start();
			}

            Auth = new AuthService(tas);

			offlineMessages = new OfflineMessages(tas);
            playerMover = new PlayerMover(tas);

			new Shuffler(tas);
			return true;
		}

        
        


		void TrackPlayerMinutes()
		{
            Directory.SetCurrentDirectory(webRoot);
			try
			{
				var now = DateTime.Now;
				if (now.Minute != lastStatsSave.Minute)
				{
					foreach (var pair in tas.ExistingBattles)
					{
						var mod = pair.Value.ModName;
						var map = pair.Value.MapName;
						var cnt = pair.Value.Users.Count;
						if (ModPlayerMinutes.ContainsKey(mod)) ModPlayerMinutes[mod] = ModPlayerMinutes[mod] + cnt;
						else ModPlayerMinutes.Add(mod, cnt);

						if (MapPlayerMinutes.ContainsKey(map)) MapPlayerMinutes[map] = MapPlayerMinutes[map] + cnt;
						else MapPlayerMinutes.Add(map, cnt);
					}

					if (!Directory.Exists("stats")) Directory.CreateDirectory("stats");

					var fname = now.ToString("yyyy-MM-dd-HH") + ".mods";
					var res = "";
					foreach (var st in ModPlayerMinutes) res += st.Key + "|" + st.Value + "\n";
					File.WriteAllText(Directory.GetCurrentDirectory() + "/stats/" + fname, res);

					fname = now.ToString("yyyy-MM-dd-HH") + ".maps";
					res = "";
					foreach (var st in MapPlayerMinutes) res += st.Key + "|" + st.Value + "\n";
					File.WriteAllText(Directory.GetCurrentDirectory() + "/stats/" + fname, res);

					if (now.Hour != lastStatsSave.Hour)
					{
						ModPlayerMinutes.Clear();
						MapPlayerMinutes.Clear();
					}

					lastStatsSave = now;
				}
			}
			catch (Exception e)
			{
				Trace.TraceError("Error while tracking stats " + e.ToString());
			}
		}

		void recon_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (tas.IsConnected && tas.IsLoggedIn) return;
			recon.Stop();
			try
			{
				tas.Connect(config.ServerHost, config.ServerPort);
			}
			catch
			{
				recon.Start();
			}
		}

		void statsTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			TrackPlayerMinutes();
		}

		void tas_Connected(object sender, TasEventArgs e)
		{
			tas.Login(config.AccountName, config.AccountPassword);
		}

		void tas_ConnectionLost(object sender, TasEventArgs e)
		{
			recon.Start();
		}

		void tas_LoginAccepted(object sender, TasEventArgs e)
		{
			for (var i = 0; i < config.JoinChannels.Length; ++i) tas.JoinChannel(config.JoinChannels[i]);
		}

		void tas_LoginDenied(object sender, TasEventArgs e)
		{
			recon.Start();
		}
	}

    public class PlayerMover
    {
        TasClient tas;

        public PlayerMover(TasClient tas)
        {
            this.tas = tas;
            tas.Said += new EventHandler<TasSayEventArgs>(tas_Said);
        }

        void tas_Said(object sender, TasSayEventArgs e)
        {
            if (e.Place == TasSayEventArgs.Places.Normal && e.Text.StartsWith("!move")) {
                var db = new ZkDataContext();
                var acc = db.Accounts.Single(x => x.Name == e.UserName);
                if (acc.IsZeroKAdmin || acc.IsLobbyAdministrator) {
                    var parts = e.Text.Split(' ');
                    if (parts.Length != 3)
                    {
                        tas.Say(TasClient.SayPlace.User, e.UserName, "!move [from] [to]", false);
                    }
                    else
                    {
                        var from = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == parts[1]);
                        var to = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == parts[2]);
                        if (from != null && to != null)
                        {
                            foreach (var b in from.Users)
                            {
                                if (!b.LobbyUser.IsInGame) tas.Say(TasClient.SayPlace.User, b.Name, "!join " + parts[2], false);
                            }
                        }
                        else {
                            tas.Say(TasClient.SayPlace.User, e.UserName, "Not a valid battle host name",false);
                        }
                    }
                }
            }

        }
    }
}