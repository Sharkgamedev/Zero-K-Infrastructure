﻿using System;
using System.Data.Linq.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class PlanetwarsController: Controller
    {
        //
        // GET: /Planetwars/
        [Auth]
        public ActionResult BombPlanet(int planetID) {
            return Content("phail");
            // hack
            /*

            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);

            var accessiblePlanets = Galaxy.DropshipAttackablePlanets(db, acc.ClanID.Value).Select(x => x.PlanetID).ToList();
            var accessible = accessiblePlanets.Any(x => x == planetID);
            var jumpgates = acc.GetJumpGateCapacity();
            var avail = accessible ?Global.Account.DropshipCount : Math.Min(jumpgates, Global.Account.DropshipCount);
            avail = Math.Min(avail, acc.GetDropshipCapacity());
            var planet = db.Planets.Single(x => x.PlanetID == planetID);
            if (Global.Nightwatch.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot bomb planet");
            if (!planet.TreatyAttackablePlanet(acc.Clan)) return Content("This is allied world");

            if (!accessible && planet.PlanetStructures.Any(x => !x.IsDestroyed && x.StructureType.EffectBlocksJumpgate == true)) return Content("Planetary defenses interdict your jumpgate");

            var defs = planet.PlanetStructures.Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectDropshipDefense) ?? 0;
            var bombNeed = GlobalConst.BaseShipsToBomb + defs / 3;

            var structs = planet.PlanetStructures.Where(x => !x.IsDestroyed && x.StructureType.IsIngameDestructible).ToList();
            if (avail >= bombNeed)
            {
                acc.DropshipCount -= bombNeed;
                if (structs.Count > 0)
                {
                    var s = structs[new Random().Next(structs.Count)];
                    if (s.StructureType.IngameDestructionNewStructureTypeID != null)
                    {
                        db.PlanetStructures.DeleteOnSubmit(s);
                        db.PlanetStructures.InsertOnSubmit(new PlanetStructure()
                                                           {
                                                               PlanetID = planet.PlanetID,
                                                               StructureTypeID = s.StructureType.IngameDestructionNewStructureTypeID.Value,
                                                               //IsDestroyed = true
                                                           });
                    }
                    else s.IsDestroyed = true;

                    foreach (var entry in planet.AccountPlanets) entry.Influence = (int)(entry.Influence*0.97);
                    db.Events.InsertOnSubmit(
                        Global.CreateEvent("{0} bombed {1} planet {2} with {3} ships, destroying {4} and reducing influence by 3%",
                                           acc,
                                           planet.Account,
                                           planet,
                                           bombNeed,
                                           s.StructureType.Name));
                }
                else
                {
                    foreach (var entry in planet.AccountPlanets) entry.Influence = (int)(entry.Influence*0.90);
                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} bombed {1} planet {2} with {3} ships, reducing influence by 10%",
                                                                acc,
                                                                planet.Account,
                                                                planet,
                                                                bombNeed));
                }
            }
            db.SubmitChanges();
            SetPlanetOwners();
            return RedirectToAction("Planet", new { id = planetID });
             * */
        }

        [Auth]
        public ActionResult BuildStructure(int planetID, int structureTypeID)
        {
            // hack
            /*
            using (var db = new ZkDataContext())
            {
                var planet = db.Planets.Single(p => p.PlanetID == planetID);
                if (Global.Nightwatch.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot build structures");
                var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                if (Global.ClanID != planet.Account.ClanID) return Content("Planet is not under your control.");
                var structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
                if (structureType == null) return Content("Structure type does not exist.");
                if (!structureType.IsBuildable) return Content("Structure is not buildable.");

                // assumes you can only build level 1 structures! if higher level structures can be built directly, we should check down the upgrade chain too
                if (StructureType.HasStructureOrUpgrades(db, planet, structureType)) return Content("Structure or its upgrades already built");

                if (acc.Credits < structureType.Cost) return Content("Insufficient credits.");
                acc.Credits -= structureType.Cost;

                var newBuilding = new PlanetStructure { StructureTypeID = structureTypeID, PlanetID = planetID };
                db.PlanetStructures.InsertOnSubmit(newBuilding);
                db.SubmitChanges();

                db.Events.InsertOnSubmit(Global.CreateEvent("{0} has built a {1} on {2}.", Global.Account, newBuilding.StructureType.Name, planet));
                SetPlanetOwners(db);
            }
            */
            return RedirectToAction("Planet", new { id = planetID });
        }

        [Auth]
        public ActionResult DestroyStructure(int planetID, int structureTypeID)
        {
            using (var db = new ZkDataContext())
            {
                var planet = db.Planets.Single(p => p.PlanetID == planetID);
                if (Global.Nightwatch.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot destroy structures");
                var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                if (Global.ClanID != planet.Account.ClanID) return Content("Planet is not under your control.");
                var structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
                if (structureType == null) return Content("Structure type does not exist.");
                if (!structureType.IsBuildable) return Content("Structure is not buildable.");

                // assumes you can only build level 1 structures! if higher level structures can be built directly, we should check down the upgrade chain too
                if (!StructureType.HasStructureOrUpgrades(db, planet, structureType)) return Content("Structure or its upgrades not present");

                var list = planet.PlanetStructures.Where(x => x.StructureTypeID == structureTypeID).ToList();
                var toDestroy = list[0];
                if (toDestroy.StructureType.IngameDestructionNewStructureTypeID != null)
                {
                    db.PlanetStructures.DeleteOnSubmit(toDestroy);
                    db.PlanetStructures.InsertOnSubmit(new PlanetStructure()
                    {
                        PlanetID = planet.PlanetID,
                        StructureTypeID = toDestroy.StructureType.IngameDestructionNewStructureTypeID.Value,
                        //IsDestroyed = true
                    });
                }
                else toDestroy.IsDestroyed = true;
                db.SubmitChanges();

                db.Events.InsertOnSubmit(Global.CreateEvent("{0} has demolished a {1} on {2}.", Global.Account, toDestroy.StructureType.Name, planet));
                SetPlanetOwners(db);
            }

            return RedirectToAction("Planet", new { id = planetID });
        }

        [Auth]
        public ActionResult RepairStructure(int planetID, int structureTypeID)
        {
            /*
            var db = new ZkDataContext();
            var planet = db.Planets.Single(p => p.PlanetID == planetID);
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (Global.ClanID != planet.Account.ClanID) return Content("Planet is not under control.");
            var structure = db.PlanetStructures.SingleOrDefault(s => s.PlanetID == planetID && s.StructureTypeID == structureTypeID);
            if (!structure.IsDestroyed) return Content("Can't repair a working structure.");
            if (acc.Credits < structure.StructureType.Cost * GlobalConst.PlanetwarsRepairCost) return Content("Insufficient credits.");
            acc.Credits -= (int)(structure.StructureType.Cost * GlobalConst.PlanetwarsRepairCost);
            structure.IsDestroyed = false;
            db.Events.InsertOnSubmit(Global.CreateEvent("{0} has repaired a {1} on {2}.", Global.Account, structure.StructureType.Name, planet));
            db.SubmitChanges();
            SetPlanetOwners(db);
            */
             // hack
             return RedirectToAction("Planet", new { id = planetID });

        }




        public ActionResult Events(int? planetID,
                                   int? accountID,
                                   int? springBattleID,
                                   int? clanID,
                                    int? factionID,
                                   string filter,
                                   int pageSize = 0,
                                   int page = 0,
                                   bool partial = false)
        {
            var db = new ZkDataContext();
            if (Request.IsAjaxRequest()) partial = true;
            if (pageSize == 0)
            {
                if (!partial) pageSize = 40;
                else pageSize = 10;
            }
            var res = db.Events.AsQueryable();
            if (planetID.HasValue) res = res.Where(x => x.EventPlanets.Any(y => y.PlanetID == planetID));
            if (accountID.HasValue) res = res.Where(x => x.EventAccounts.Any(y => y.AccountID == accountID));
            if (clanID.HasValue) res = res.Where(x => x.EventClans.Any(y => y.ClanID == clanID));
            if (springBattleID.HasValue) res = res.Where(x => x.EventSpringBattles.Any(y => y.SpringBattleID == springBattleID));
            if (factionID.HasValue) res = res.Where(x => x.EventFactions.Any(y => y.FactionID == factionID));
            if (!string.IsNullOrEmpty(filter)) res = res.Where(x => x.Text.Contains(filter));
            res = res.OrderByDescending(x => x.EventID);

            var ret = new EventsResult
                      {
                          PageCount = (res.Count()/pageSize) + 1,
                          Page = page,
                          Events = res.Skip(page*pageSize).Take(pageSize),
                          PlanetID = planetID,
                          AccountID = accountID,
                          SpringBattleID = springBattleID,
                          Filter = filter,
                          ClanID = clanID,
                          Partial = partial,
                          PageSize = pageSize
                      };

            return View(ret);
        }


        public Bitmap GenerateGalaxyImage(int galaxyID, double zoom = 1, double antiAliasingFactor = 4)
        {
            zoom *= antiAliasingFactor;
            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

                using (var background = Image.FromFile(Server.MapPath("/img/galaxies/" + gal.ImageName)))
                {
                    var im = new Bitmap((int)(background.Width*zoom), (int)(background.Height*zoom));
                    using (var gr = Graphics.FromImage(im))
                    {
                        gr.DrawImage(background, 0, 0, im.Width, im.Height);

                        /*
						using (var pen = new Pen(Color.FromArgb(255, 180, 180, 180), (int)(1*zoom)))
						{
							foreach (var l in gal.Links)
							{
								gr.DrawLine(pen,
								            (int)(l.PlanetByPlanetID1.X*im.Width),
								            (int)(l.PlanetByPlanetID1.Y*im.Height),
								            (int)(l.PlanetByPlanetID2.X*im.Width),
								            (int)(l.PlanetByPlanetID2.Y*im.Height));
							}
						}*/

                        foreach (var p in gal.Planets)
                        {
                            string planetIconPath = null;
                            try
                            {
                                planetIconPath = "/img/planets/" + p.Resource.MapPlanetWarsIcon;
                                using (var pi = Image.FromFile(Server.MapPath(planetIconPath)))
                                {
                                    var aspect = pi.Height/(double)pi.Width;
                                    var width = (int)(p.Resource.PlanetWarsIconSize*zoom);
                                    var height = (int)(width*aspect);
                                    gr.DrawImage(pi, (int)(p.X*im.Width) - width/2, (int)(p.Y*im.Height) - height/2, width, height);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new ApplicationException(
                                    string.Format("Cannot process planet image {0} for planet {1} map {2}",
                                                  planetIconPath,
                                                  p.PlanetID,
                                                  p.MapResourceID),
                                    ex);
                            }
                        }
                        if (antiAliasingFactor == 1) return im;
                        else
                        {
                            zoom /= antiAliasingFactor;
                            return im.GetResized((int)(background.Width*zoom), (int)(background.Height*zoom), InterpolationMode.HighQualityBicubic);
                        }
                    }
                }
            }
        }

     
        public ActionResult Index(int? galaxyID = null)
        {
            var db = new ZkDataContext();

            Galaxy gal;
            if (galaxyID != null) gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
            else gal = db.Galaxies.Single(x => x.IsDefault);

            var cachePath = Server.MapPath(string.Format("/img/galaxies/render_{0}.jpg", gal.GalaxyID));
            if (gal.IsDirty || !System.IO.File.Exists(cachePath))
            {
                using (var im = GenerateGalaxyImage(gal.GalaxyID))
                {
                    im.SaveJpeg(cachePath, 85);
                    gal.IsDirty = false;
                    gal.Width = im.Width;
                    gal.Height = im.Height;
                    db.SubmitChanges();
                }
            }
            return View("Galaxy", gal);
        }




        public ActionResult Minimap()
        {
            var db = new ZkDataContext();

            return View(db.Galaxies.Single(g => g.IsDefault));
        }




        public ActionResult Planet(int id)
        {
            var db = new ZkDataContext();
            var planet = db.Planets.Single(x => x.PlanetID == id);
            if (planet.ForumThread != null)
            {
                planet.ForumThread.UpdateLastRead(Global.AccountID, false);
                db.SubmitChanges();
            }
            return View(planet);
        }

        [Auth]
        public ActionResult SendDropships(int planetID, int count)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            var planet = db.Planets.SingleOrDefault(x => x.PlanetID == planetID);
            var there = planet.AccountPlanets.Where(x => x.AccountID == acc.AccountID).Sum(x => (int?)x.DropshipCount) ?? 0;
            var accessiblePlanets = Galaxy.DropshipAttackablePlanets(db, acc.ClanID.Value).Select(x => x.PlanetID).ToList();
            var accessible = accessiblePlanets.Any(x => x == planetID);
            if (!accessible)
            {
                int jumpGateCapacity = acc.GetJumpGateCapacity();
                if (there + count > jumpGateCapacity) return Content(string.Format("Tha planet cannot be accessed via wormholes and your jumpgates are at capacity - you can maintain {0} ships using jumpgates", jumpGateCapacity));
            }
            var cnt = Math.Max(count, 0);
            
            
            if (!planet.TreatyAttackablePlanet(acc.Clan)) return Content("This is allied world");


            if (!accessible && planet.PlanetStructures.Any(x => !x.IsDestroyed && x.StructureType.EffectBlocksJumpgate == true)) return Content("Planetary defenses interdict your jumpgate");
            var capa = acc.GetDropshipCapacity();
            
            if (cnt + there > capa) return Content("Too many ships, increase fleet size");
            cnt = Math.Min(cnt, (int)acc.DropshipCount);
            if (cnt > 0)
            {
                acc.DropshipCount = (acc.DropshipCount) - cnt;

                if (Global.Nightwatch.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot send ships");

                if (planet.Account != null)
                {
                    AuthServiceClient.SendLobbyMessage(planet.Account,
                                                       string.Format(
                                                           "Warning: long range scanners detected fleet of {0} ships inbound to your planet {1} http://zero-k.info/Planetwars/Planet/{2}",
                                                           cnt,
                                                           planet.Name,
                                                           planet.PlanetID));
                }
                var pac = acc.AccountPlanets.SingleOrDefault(x => x.PlanetID == planetID);
                if (pac == null)
                {
                    pac = new AccountPlanet { AccountID = Global.AccountID, PlanetID = planetID };
                    db.AccountPlanets.InsertOnSubmit(pac);
                }
                pac.DropshipCount += cnt;
                if (cnt > 0) db.Events.InsertOnSubmit(Global.CreateEvent("{0} sends {1} dropships to {2}", acc, cnt, planet));
                db.SubmitChanges();
            }
            return RedirectToAction("Planet", new { id = planetID });
        }

        public ActionResult RunSetPlanetOwners()
        {
            using (var db = new ZkDataContext()) SetPlanetOwners(db);
            return Content("Done.");
        }


        /// <summary>
        /// Updates shadow influence and new owners
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sb">optional spring batle that caused this change (for event logging)</param>
        public static void SetPlanetOwners(ZkDataContext db = null, SpringBattle sb = null)
        {
            if (db == null) db = new ZkDataContext();
            Galaxy.RecalculateShadowInfluence(db);
            var havePlanetsChangedHands = false;

            var gal = db.Galaxies.Single(x => x.IsDefault);
            foreach (var planet in gal.Planets)
            {
                //if (planet.PlanetID == 2274) Debugger.Break();
                var currentOwnerClanID = planet.Account != null ? planet.Account.ClanID : null;
                var currentOwnerFactionID = planet.Account != null ? planet.Account.FactionID : null;


                var mostInfluentiaFactionEntry =
                    planet.AccountPlanets.GroupBy(ap => ap.Account.Faction).Where(x => x.Key != null).Select(
                        x => new { Faction = x.Key, FactionInfluence = (int?)x.Sum(y => y.Influence + y.ShadowInfluence) ?? 0 }).OrderByDescending(
                            x => x.FactionInfluence).FirstOrDefault();

                
                // in case of a tie when deciding which CLAN to get a planet - give to one with less planets
                ClanEntry mostInfluentialClanEntry = null;
                if (mostInfluentiaFactionEntry != null &&
                    (mostInfluentiaFactionEntry.Faction.FactionID == currentOwnerFactionID ||
                     mostInfluentiaFactionEntry.FactionInfluence >  planet.GetIPToCapture() ))
                {
                    mostInfluentialClanEntry =
                        planet.AccountPlanets.Where(
                            x => x.Account.FactionID == mostInfluentiaFactionEntry.Faction.FactionID && x.Account.ClanID != null).GroupBy(
                                x => x.Account.Clan).Select(x => new ClanEntry(x.Key, (int?)x.Sum(y => y.Influence) ?? 0)).OrderByDescending(x => x.ClanInfluence).ThenBy(y => y.Clan.Accounts.Sum(z => z.Planets.Count())).FirstOrDefault();
                }

                /*
                if ((mostInfluentialClanEntry == null || mostInfluentialClanEntry.Clan == null || mostInfluentialClanEntry.ClanInfluence == 0) &&
                    planet.Account != null)
                {
                    // disown the planet, nobody has right to own it atm
                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} of {2} has abandoned planet {1}. {3}",
                                                                planet.Account,
                                                                planet,
                                                                planet.Account.Clan,
                                                                sb));
                    planet.Account = null;
                    havePlanetsChangedHands = true;
                }
                else */if (mostInfluentialClanEntry != null &&  mostInfluentialClanEntry.Clan.ClanID != currentOwnerClanID && (currentOwnerClanID== null || currentOwnerFactionID != mostInfluentiaFactionEntry.Faction.FactionID || mostInfluentialClanEntry.ClanInfluence > planet.AccountPlanets.Where(x=>x.Account.ClanID == currentOwnerClanID).Sum(x=>x.Influence)))
                {
                    // planet changes owner, most influential clan is not current owner and has more ip to capture than needed

                    havePlanetsChangedHands = true;

                    foreach (var structure in planet.PlanetStructures.Where(structure => structure.StructureType.OwnerChangeDeletesThis).ToList()) planet.PlanetStructures.Remove(structure); //  delete structure

                    // find who will own it
                    // in case of a tie when deciding which PLAYER to get a planet - give it to one with least planets
                    var mostInfluentialPlayer =
                        planet.AccountPlanets.Where(x => x.Account.ClanID == mostInfluentialClanEntry.Clan.ClanID).OrderByDescending(
                            x => x.Influence).ThenBy(x => x.Account.Planets.Count()).First().Account;

                    var firstPlanet = !mostInfluentialPlayer.Planets.Any();

                    if (planet.OwnerAccountID == null) // no previous owner
                    {
                        planet.Account = mostInfluentialPlayer;
                        db.Events.InsertOnSubmit(Global.CreateEvent("{0} has claimed planet {1} for {2}. {3}",
                                                                    mostInfluentialPlayer,
                                                                    planet,
                                                                    mostInfluentialClanEntry.Clan,
                                                                    sb));
                        AuthServiceClient.SendLobbyMessage(mostInfluentialPlayer,
                                                           string.Format(
                                                               "Congratulations, you now own planet {0}!! http://zero-k.info/PlanetWars/Planet/{1}",
                                                               planet.Name,
                                                               planet.PlanetID));
                    }
                    else
                    {
                        db.Events.InsertOnSubmit(Global.CreateEvent("{0} of {3} has captured planet {1} from {2} of {4}. {5}",
                                                                    mostInfluentialPlayer,
                                                                    planet,
                                                                    planet.Account,
                                                                    mostInfluentialClanEntry.Clan,
                                                                    planet.Account.Clan,
                                                                    sb));

                        AuthServiceClient.SendLobbyMessage(mostInfluentialPlayer,
                                                           string.Format(
                                                               "Congratulations, you now own planet {0}!! http://zero-k.info/PlanetWars/Planet/{1}",
                                                               planet.Name,
                                                               planet.PlanetID));
                        AuthServiceClient.SendLobbyMessage(planet.Account,
                                                           string.Format(
                                                               "Warning, you just lost planet {0}!! http://zero-k.info/PlanetWars/Planet/{1}",
                                                               planet.Name,
                                                               planet.PlanetID));

                        planet.Account = mostInfluentialPlayer;

                    }

                    // return dropshuips home if owner is ceasefired/allied/same faction
                    foreach (var entry in planet.AccountPlanets.Where(x => x.DropshipCount > 0))
                    {
                        if (entry.Account.FactionID == planet.Account.FactionID || planet.Account.Clan.GetEffectiveTreaty(entry.Account.Clan).AllyStatus >= AllyStatus.Ceasefire)
                        {
                            entry.Account.DropshipCount += entry.DropshipCount;
                            entry.DropshipCount = 0;
                        }
                    }

                }
            }

            db.SubmitChanges();
            if (havePlanetsChangedHands) SetPlanetOwners(db, sb); // we need another cycle because of shadow influence chain reactions
        }


       
        [Auth]
        public ActionResult SubmitRenamePlanet(int planetID, string newName)
        {
            using (var scope = new TransactionScope())
            {
                if (String.IsNullOrWhiteSpace(newName)) return Content("Error: the planet must have a name.");
                var db = new ZkDataContext();
                var planet = db.Planets.Single(p => p.PlanetID == planetID);
                if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Unauthorized");
                db.SubmitChanges();
                db.Events.InsertOnSubmit(Global.CreateEvent("{0} renamed planet {1} from {2} to {3}", Global.Account, planet, planet.Name, newName));
                planet.Name = newName;
                db.SubmitChanges();
                scope.Complete();
                return RedirectToAction("Planet", new { id = planet.PlanetID });
            }
        }

        [Auth]
        public ActionResult UpgradeStructure(int planetID, int structureTypeID)
        {
            using (var scope = new TransactionScope())
            {
                var db = new ZkDataContext();
                var planet = db.Planets.Single(p => p.PlanetID == planetID);
                var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                if (Global.ClanID != planet.Account.ClanID) return Content("Planet is not under control.");
                var oldStructure = db.PlanetStructures.SingleOrDefault(s => s.PlanetID == planetID && s.StructureTypeID == structureTypeID);
                if (oldStructure == null) return Content("Structure does not exist");
                if (oldStructure.StructureType.UpgradesToStructureID == null) return Content("Structure can't be upgraded.");
                if (oldStructure.IsDestroyed) return Content("Can't upgrade a destroyed structure");

                var newStructureType = db.StructureTypes.Single(s => s.StructureTypeID == oldStructure.StructureType.UpgradesToStructureID);
                if (acc.Credits < newStructureType.Cost) return Content("Insufficient credits.");
                acc.Credits -= newStructureType.Cost;

                var newStructure = new PlanetStructure { PlanetID = planetID, StructureTypeID = newStructureType.StructureTypeID };

                db.PlanetStructures.InsertOnSubmit(newStructure);
                db.PlanetStructures.DeleteOnSubmit(oldStructure);

                db.SubmitChanges();
                db.Events.InsertOnSubmit(Global.CreateEvent("{0} has built a {1} on {2}.", Global.Account, newStructure.StructureType.Name, planet));

                db.SubmitChanges();
                SetPlanetOwners(db);

                scope.Complete();
            }
            return RedirectToAction("Planet", new { id = planetID });
        }


        public class ClanEntry
        {
            readonly Clan clan;
            readonly int clanInfluence;
            public Clan Clan { get { return clan; } }
            public int ClanInfluence { get { return clanInfluence; } }
            public int ShadowInfluence;

            public ClanEntry(Clan clan, int clanInfluence)
            {
                this.clan = clan;
                this.clanInfluence = clanInfluence;
            }
        }

        public ActionResult RecallRole(int accountID, int roletypeID) {
            var db = new ZkDataContext();
            var targetAccount = db.Accounts.Single(x => x.AccountID == accountID);
            var myAccount = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            var role = db.RoleTypes.Single(x => x.RoleTypeID == roletypeID);
            if (myAccount.CanRecall(targetAccount, role)) {
                db.AccountRoles.DeleteAllOnSubmit(db.AccountRoles.Where(x=>x.AccountID == accountID && x.RoleTypeID == roletypeID));
                db.Events.InsertOnSubmit(Global.CreateEvent("{0} was recalled from the {1} role of {2} by {3}", targetAccount, role.IsClanOnly ? (object)myAccount.Clan : myAccount.Faction, role, myAccount));
                AuthServiceClient.SendLobbyMessage(targetAccount, string.Format("You were recalled from the function of {0} by {1}", role.Name, myAccount.Name));
                db.SubmitAndMergeChanges();
                return RedirectToAction("Detail", "Users", new { id = accountID });
            }  else {
                return Content("Cannot recall");
            }
        }

        public ActionResult AppointRole(int accountID, int roletypeID)
        {
            var db = new ZkDataContext();
            var targetAccount = db.Accounts.Single(x => x.AccountID == accountID);
            var myAccount = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            var role = db.RoleTypes.Single(x => x.RoleTypeID == roletypeID);
            if (myAccount.CanAppoint(targetAccount, role)) {
                Account previous = null;
                if (role.IsOnePersonOnly)
                {
                    var entries = db.AccountRoles.Where(x => x.RoleTypeID == role.RoleTypeID && (x.FactionID == myAccount.FactionID || x.ClanID == myAccount.ClanID)).ToList();
                    if (entries.Any())
                    {
                        previous = entries.First().AccountByAccountID;
                        db.AccountRoles.DeleteAllOnSubmit(entries);
                    }
                }
                var entry = new AccountRole()
                {
                    AccountID = accountID,
                    Inauguration = DateTime.UtcNow,
                    Clan = role.IsClanOnly ? myAccount.Clan : null,
                    Faction = !role.IsClanOnly ? myAccount.Faction : null,
                    RoleTypeID = roletypeID,
                };
                db.AccountRoles.InsertOnSubmit(entry);
                if (previous != null) db.Events.InsertOnSubmit(Global.CreateEvent("{0} was appointed to the {1} role of {2} by {3} - replacing {4}", targetAccount, role.IsClanOnly ? (object)myAccount.Clan : myAccount.Faction, role, myAccount, previous));
                else db.Events.InsertOnSubmit(Global.CreateEvent("{0} was appointed to the {1} role of {2} by {3}", targetAccount, role.IsClanOnly ? (object)myAccount.Clan : myAccount.Faction, role, myAccount));
                AuthServiceClient.SendLobbyMessage(targetAccount, string.Format("You were appointed for the function of {0} by {1}", role.Name, myAccount.Name));
                db.SubmitAndMergeChanges();
                return RedirectToAction("Detail", "Users", new { id = accountID });
            }
            else
            {
                return Content("Cannot recall");
            }
        }

    }

    public class EventsResult
    {
        public int? AccountID;
        public int? ClanID;
        public IQueryable<Event> Events;
        public string Filter;
        public int Page;
        public int PageCount;
        public int PageSize;
        public bool Partial;
        public int? PlanetID;
        public int? SpringBattleID;
    }
}