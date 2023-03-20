using EXILED.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MegapatchII_SCP_Regen_Exiled
{
    public class Plugin : EXILED.Plugin
    {
        public override string getName => "TFP-SCP-Heal-MP2";

        private Dictionary<string, CancellationTokenSource> playerHealingThreads = new Dictionary<string, CancellationTokenSource>();

        public override void OnDisable()
        {
            playerHealingThreads.Clear();
            EXILED.Events.SetClassEvent -= ClassUpdate;
            EXILED.Events.PlayerLeaveEvent -= PlayerLeft;
        }

        public override void OnEnable()
        {
            EXILED.Events.SetClassEvent += ClassUpdate;
            EXILED.Events.PlayerLeaveEvent += PlayerLeft;
        }

        private void PlayerLeft(EXILED.PlayerLeaveEvent ev)
        {
            try
            {
                ServerConsole.AddLog($"[SCPHeal] left. {ev.Player.GetUserId()}");
                playerHealingThreads[ev.Player.GetUserId()].Cancel();
                playerHealingThreads.Remove(ev.Player.GetUserId());
            }
            catch { }
        }

        private void ClassUpdate(EXILED.SetClassEvent ev)
        {
            ServerConsole.AddLog($"[SCPHeal] role change. {ev.Player.GetUserId()} to {ev.Role} (is scp: {ev.Role.IsAnyScp()})");
            if (ev.Role.IsAnyScp())
            {
                var cts = new CancellationTokenSource();
                var thrd = new Thread(() => SCPHealingCoroutine(ev.Player, cts.Token));
                thrd.Start();
                playerHealingThreads.Add(ev.Player.GetUserId(), cts);
            }
        }

        public override void OnReload()
        {
            OnDisable();
            OnEnable();
        }

        private async Task SCPHealingCoroutine(ReferenceHub pl, CancellationToken cancellationToken)
        {
            ServerConsole.AddLog("[SCPHeal] coroutine started");
            try
            {
                bool didPlayerMoveLastOpportunity = false;
                Vector3 oldpos = Vector3.zero;
                Vector3 newpos = Vector3.zero;

                while (true)
                {
                    await Task.Delay(10000);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        ServerConsole.AddLog("[SCPHeal] calcellation requested");
                        return;
                    }

                    newpos = pl.GetPosition();
                    ServerConsole.AddLog($"heal att started. vals: pos same => {newpos == oldpos}, newpos => {newpos}, oldpos => {oldpos}");

                    if (newpos == oldpos)
                    {
                        ServerConsole.AddLog($"isSCP? {pl.GetRole().IsAnyScp()} ({pl.GetRole()})");
                        if (pl.GetRole().IsAnyScp())
                        {
                            switch (pl.GetRole())
                            {
                                case RoleType.Scp173:
                                    pl.Heal(50);
                                    break;
                                case RoleType.Scp096:
                                    pl.Heal(45);
                                    break;
                                case RoleType.Scp049:
                                    pl.Heal(40);
                                    break;
                                case RoleType.Scp0492:
                                    pl.Heal(50);
                                    break;
                                case RoleType.Scp106:
                                    pl.Heal(8);
                                    break;
                                case RoleType.Scp93953:
                                    pl.Heal(35);
                                    break;
                                case RoleType.Scp93989:
                                    pl.Heal(35);
                                    break;
                                default:
                                    //do nothing, most likely player is 079
                                    break;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    oldpos = newpos;
                }
            }
            catch
            {
#if DEBUG
                ServerConsole.AddLog($"[SCPHeal] thread crashed (this is sad)");
#endif
            }
        }
    }
}
