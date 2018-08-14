﻿using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Globalization;
using UnityEngine;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class SevenDaysPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly ClientInfo client;

        internal SevenDaysPlayer(ulong id, string name)
        {
            if (libPerms == null)
            {
                libPerms = Interface.Oxide.GetLibrary<Permission>();
            }

            Name = name.Sanitize();
            Id = id.ToString();
        }

        internal SevenDaysPlayer(ClientInfo client) : this(client.steamId.m_SteamID, client.playerName)
        {
            this.client = client;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        public object Object => GameManager.Instance.World.Players.dict[client.entityId];

        /// <summary>
        /// Gets the player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion Objects

        #region Information

        /// <summary>
        /// Gets/sets the name for the player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address => client.networkPlayer.ipAddress;

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping => client.ping;

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin => GameManager.Instance.adminTools.IsAdmin(client.playerId);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned => GameManager.Instance.adminTools.IsBanned(Id);

        /// <summary>
        /// Gets if the player is connected
        /// </summary>
        public bool IsConnected => ConnectionManager.Instance.GetClientInfoForPlayerId(Id) != null;

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping
        {
            get
            {
                EntityPlayer entity = Object as EntityPlayer;
                return entity != null && entity.IsSleeping;
            }
        }

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        public bool IsServer => false;

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (!IsBanned)
            {
                // Ban player with reason
                GameManager.Instance.adminTools.AddBan(Id, null, new DateTime(duration.Ticks), reason);

                // Kick player if connected
                if (IsConnected)
                {
                    Kick(reason);
                }
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining
        {
            get
            {
                AdminToolsClientInfo adminClient = GameManager.Instance.adminTools.GetAdminToolsClientInfo(Id);
                return adminClient.BannedUntil.TimeOfDay;
            }
        }

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount)
        {
            EntityPlayer entity = Object as EntityPlayer;
            entity?.AddHealth((int)amount);
        }

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        public float Health
        {
            get
            {
                EntityPlayer entity = Object as EntityPlayer;
                return entity?.Health ?? 0f;
            }
            set
            {
                EntityPlayer entity = Object as EntityPlayer;
                if (entity != null)
                {
                    entity.Stats.Health.Value = value;
                }
            }
        }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            EntityPlayer entity = Object as EntityPlayer;
            entity?.DamageEntity(new DamageSource(EnumDamageSourceType.Undef), (int)amount, false);
        }

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            GameUtils.KickPlayerData kickData = new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, DateTime.Now, reason);
            GameUtils.KickPlayerForClientInfo(client, kickData);
        }

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        public void Kill()
        {
            EntityPlayer entity = Object as EntityPlayer;
            entity?.Kill(DamageResponse.New(new DamageSource(EnumDamageSourceType.Undef), true));
        }

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        public float MaxHealth
        {
            get
            {
                EntityPlayer entity = Object as EntityPlayer;
                return entity?.GetMaxHealth() ?? 0f;
            }
            set
            {
                EntityPlayer entity = Object as EntityPlayer;
                if (entity != null)
                {
                    entity.Stats.Health.BaseMax = value;
                }
            }
        }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            EntityPlayer entity = Object as EntityPlayer;
            entity?.SetPosition(new Vector3(x, y, z));
        }

        /// <summary>
        /// Teleports the player's character to the specified generic position
        /// </summary>
        /// <param name="pos"></param>
        public void Teleport(GenericPosition pos) => Teleport(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban()
        {
            // Check if unbanned already
            if (IsBanned)
            {
                // Set to unbanned
                GameManager.Instance.adminTools.RemoveBan(Id);
            }
        }

        #endregion Administration

        #region Location

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            EntityPlayer entity = Object as EntityPlayer;
            Vector3 pos = entity?.gameObject.transform.position ?? new Vector3();
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            EntityPlayer entity = Object as EntityPlayer;
            Vector3 pos = entity?.transform.position ?? new Vector3();
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion Location

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message and prefix to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(string message, string prefix, params object[] args)
        {
            message = args.Length > 0 ? string.Format(Formatter.ToRoKAnd7DTD(message), args) : Formatter.ToRoKAnd7DTD(message);
            string formatted = prefix != null ? $"{prefix} {message}" : message;

            client.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, formatted, null, false, null, false));
        }

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => Message(message, null);

        /// <summary>
        /// Replies to the player with the specified message and prefix
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(string message, string prefix, params object[] args) => Message(message, prefix, args);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message, null);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            SdtdConsole.Instance.ExecuteSync($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", client);
        }

        #endregion Chat and Commands

        #region Permissions

        /// <summary>
        /// Gets if the player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm) => libPerms.UserHasPermission(Id, perm);

        /// <summary>
        /// Grants the specified permission on this player
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this player
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if the player belongs to the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds the player to the specified group
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes the player from the specified group
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group) => libPerms.RemoveUserGroup(Id, group);

        #endregion Permissions

        #region Operator Overloads

        /// <summary>
        /// Returns if player's unique ID is equal to another player's unique ID
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPlayer other) => Id == other?.Id;

        /// <summary>
        /// Returns if player's object is equal to another player's object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is IPlayer && Id == ((IPlayer)obj).Id;

        /// <summary>
        /// Gets the hash code of the player's unique ID
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Returns a human readable string representation of this IPlayer
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Covalence.SevenDaysPlayer[{Id}, {Name}]";

        #endregion Operator Overloads
    }
}
