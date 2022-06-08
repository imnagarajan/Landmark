using System.Collections.Generic;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;

namespace Landmark
{
    [ApiVersion(2, 1)]
    public class Landmark : TerrariaPlugin
    {
        private static Dictionary<string, float[]> deathPos;
        private static Dictionary<string, Dictionary<string, float[]>> landmarks;

        public Landmark(Main game) : base(game)
        {
            deathPos = new Dictionary<string, float[]>();
            landmarks = new Dictionary<string, Dictionary<string, float[]>>();
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(Permissions.canchat, landmarkCommand, "landmark", "ld"));
            Commands.ChatCommands.Add(new Command(Permissions.canchat, backCommand, "back", "bk"));

            ServerApi.Hooks.NetGetData.Register(this, onPlayerDeath);
            ServerApi.Hooks.NetGreetPlayer.Register(this, onPlayerJoin);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, onPlayerDeath);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, onPlayerJoin);
            }

            base.Dispose(disposing);
        }

        void onPlayerDeath(GetDataEventArgs args)
        {
            if (args.Handled)
            {
                return;
            }

            if (args.MsgID == PacketTypes.PlayerDeathV2)
            {
                var player = TShock.Players[args.Msg.whoAmI];
                deathPos[player.UUID] = new float[]{player.X, player.Y};

                args.Handled = true;
                return;
            }
        }

        void onPlayerJoin(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            landmarks[player.UUID] = new Dictionary<string, float[]>();
        }

        void landmarkCommand(CommandArgs args)
        {
            var player = args.Player;
            var UUID = player.UUID;
            // TODO: NOT SERVER
            if (player == null)
            {
                return;
            }

            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                printLandmarkHelp(player);
                return;
            }

            string landmarkName = null;
            if (args.Parameters.Count == 2)
            {
                landmarkName = args.Parameters[1];
            }

            var cmd = args.Parameters[0];

            switch(cmd) {
                case "to":
                    if(landmarkName == null)
                    {
                        printLandmarkHelp(player);
                        return;
                    }
                    teleportToLandmark(player, landmarkName);
                    break;
                case "add":
                    if (landmarkName == null)
                    {
                        printLandmarkHelp(player);
                        return;
                    }
                    addLandmark(player, landmarkName);
                    break;
                case "del":
                    if (landmarkName == null)
                    {
                        printLandmarkHelp(player);
                        return;
                    }
                    deleteLandmark(player, landmarkName);
                    break;
                case "clear":
                    clearLandmarks(player);
                    break;
                case "list":
                    printLandmarks(player);
                    break;
                case "help":
                    printLandmarkHelp(player);
                    break;
                default:
                    if (cmd != "add" && cmd != "del" && cmd != "list" && cmd != "help" && cmd != "clear")
                    {
                        teleportToLandmark(player, cmd);
                    }
                    else
                    {
                        printLandmarkHelp(player);
                    }

                    break;
            }
        }

        void teleportToLandmark(TSPlayer player, string landmarkName)
        {
            var UUID = player.UUID;

            if (landmarks[UUID].ContainsKey(landmarkName))
            {
                var pos = landmarks[UUID][landmarkName];
                player.Teleport(pos[0], pos[1]);
                player.SendSuccessMessage($"Successfully teleported to '{landmarkName}' landmark!");
            }
            else
            {
                player.SendErrorMessage($"Landmark '{landmarkName}' does not exist.");
            }
        }

        void addLandmark(TSPlayer player, string landmarkName)
        {
            var UUID = player.UUID;

            if (landmarks[UUID].ContainsKey(landmarkName))
            {
                player.SendWarningMessage("Landmarks will be covered.");
            }

            landmarks[UUID][landmarkName] = new float[] { player.X, player.Y };
            player.SendSuccessMessage($"Landmark '{landmarkName}' added successfully!");
        }

        void deleteLandmark(TSPlayer player, string landmarkName)
        {
            var UUID = player.UUID;

            if (landmarks[UUID].ContainsKey(landmarkName))
            {
                landmarks[UUID].Remove(landmarkName);
                player.SendSuccessMessage($"Landmark '{landmarkName}' deleted successfully!");
            }
            else
            {
                player.SendErrorMessage($"Landmark '{landmarkName}' does not exist.");
            }
        }

        void clearLandmarks(TSPlayer player)
        {
            var UUID = player.UUID;
            landmarks[UUID].Clear();

            player.SendSuccessMessage("All landmarks have been cleared.");
        }

        void printLandmarks(TSPlayer player)
        {
            var UUID = player.UUID;

            if (landmarks[UUID].Keys.Count == 0)
            {
                player.SendErrorMessage("no landmarks.");
                return;
            }
            player.SendInfoMessage($"Landmarks: {string.Join(" | ", landmarks[UUID].Keys)}");
        }

        void printLandmarkHelp(TSPlayer player)
        {
            player.SendInfoMessage("----- Landmark -----");
            player.SendInfoMessage("/ld <MarkName> - Teleport to landmark");
            player.SendInfoMessage("/ld to  <MarkName> - Teleport to landmark");
            player.SendInfoMessage("/ld add <MarkName> - Add a landmark");
            player.SendInfoMessage("/ld del <MarkName> - Delete a landmark");
            player.SendInfoMessage("/ld list - List landmarks");
            player.SendInfoMessage("/ld clear - Clear landmarks");
            player.SendInfoMessage("/ld help - Landmark help");
            player.SendInfoMessage("----- Other -----");
            player.SendInfoMessage("/bk - Return to the place of death");
        }

        void backCommand(CommandArgs args)
        {
            var player = args.Player;
            // TODO: NOT SERVER
            if (player == null)
            {
                return;
            }

            if (deathPos.ContainsKey(player.UUID))
            {
                var pos = deathPos[player.UUID];
                player.Teleport(pos[0], pos[1]);
                player.SendSuccessMessage("You have successfully returned.");
            }
            else
            {
                player.SendErrorMessage("Cannot return, you have not died yet.");
            }
        }
    }
}
