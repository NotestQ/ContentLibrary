using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyceliumNetworking;
using Photon.Pun;
using Steamworks;
using UnityEngine;

namespace ContentLibrary
{
    public static class ContentLibrary
    {
        #region Main

        public static List<ContentEvent>? EventList;
        // Holds what events should be assigned or synced until you enter a lobby
        public static List<ContentEvent> TemporaryEventList = new List<ContentEvent>();

        /*
         * If player is the host this method assings events IDs and syncs them with the lobby data
         * Otherwise, this method gets the events IDs from the lobby data
         */
        internal static void OnLobbyEntered()
        {
            CLogger.LogDebug($"Lobby joined, temporary event list count: {TemporaryEventList.Count}");
            EventList = new List<ContentEvent>(TemporaryEventList.Count);
            if (MyceliumNetwork.IsHost)
            {
                for (var index = 0; index < TemporaryEventList.Count; index++)
                {
                    // Game's base IDs go from 1000 ~ 1030 so we start from 2000!  
                    ContentEvent contentEvent = TemporaryEventList[index];
                    EventList.Add(contentEvent);
                    ushort id = (ushort)(2000 + EventList.Count);
                    CLogger.LogDebug($"Added ContentEvent index {index}, type name {contentEvent.GetType().Name}, id {id}");
                    MyceliumNetwork.SetLobbyData("ContentLibrary_" + contentEvent.GetType().Name, id);
                }
                return;
            }

            for (var index = 0; index < TemporaryEventList.Count; index++)
            {
                ContentEvent contentEvent = TemporaryEventList[index];
                ushort id = MyceliumNetwork.GetLobbyData<ushort>("ContentLibrary_" + contentEvent.GetType().Name);
                CLogger.LogDebug($"Added ContentEvent index {index}, type name {contentEvent.GetType().Name}, id {id}");
                EventList[id - 2000] = contentEvent;
            }
        }

        // Call this on Awake()
        public static void AssignEvent(ContentEvent contentEvent)
        {
            // We probably don't need this if every mod assigns an event at the same time, but this is just a guarantee
            MyceliumNetwork.RegisterLobbyDataKey("ContentLibrary_" + contentEvent.GetType().Name);
            // Holds the contentEvents in any order, unsynced
            TemporaryEventList.Add(contentEvent);
        }

        // Call this on your content event's GetID
        public static ushort GetEventID(string contentEventName)
        {
            return (ushort)(2000 + EventList!.FindIndex(match => match.GetType().Name == contentEventName));
        }

        public static Photon.Realtime.Player? GetPlayerWithCamera()
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (!GlobalPlayerData.TryGetPlayerData(player, out var globalPlayerData)) continue;

                if (globalPlayerData.inventory.GetItems().Any(item => item.item.name == "Camera"))
                {
                    return player;
                }
            }

            return null;
        }

        #endregion

        #region Extras

        public static List<ContentProvider> ProviderList = new List<ContentProvider>();

        // Call this on Awake() if you want this library to handle provider construction and replication
        public static void AssignProvider(ContentProvider contentProvider)
        {
            ProviderList.Add(contentProvider);
        }

        public static ContentProvider GetContentProviderFromName(string contentProviderName)
        {
            return ProviderList.Find(match => match.GetType().Name == contentProviderName);
        }

        /*
         * This handles polling a provider and replicating it for you, it makes heavy use of the Activator() and writes your arguments
         * to a buffer so it can go through an RPC. *This CAN be done better if you do it yourself* as you'd know exactly what
         * arguments you want to pass to your provider, however if you don't really care this is perfect for you!
         */
        public static void PollAndReplicateProvider(ContentProvider contentProvider, params object[] arguments)
        {
            // From prior testing I'm sure only the camera man needs to create the provider
            var player = GetPlayerWithCamera();

            if (player == null) return;

            var componentInParent = (ContentProvider)Activator.CreateInstance(contentProvider.GetType(), arguments);

            if (player.IsLocal)
            {
                CLogger.LogDebug("Local player is the one holding the camera, creating provider...");

                ContentPolling.contentProviders.Add(componentInParent, 1);
            }
            else
            {
                CLogger.LogDebug("Local player is not the one holding the camera");

                CSteamID steamID;
                bool idSuccess = SteamAvatarHandler.TryGetSteamIDForPlayer(player, out steamID);
                if (idSuccess == false) {
                    return; 
                }

                CLogger.LogDebug("Got steamID successfully");

                ContentPlugin.RPCTargetRelay("ReplicatePollProvider", steamID, contentProvider.GetType().Name, arguments);
                // Just to make sure, we still poll a provider, yes this is dumb but I have yet to test it without this
                ContentPolling.contentProviders.Add(componentInParent, 1);
            }
            
        }

        /*
         * I still don't know how to synchronize gameObjects through players
         * Monsters have Photon.ViewIDs and normal items don't have NetworkIdentity
         * Though in ItemInstance they have m_guid which can be used to replicate them
         * What should we expect from the end user (developer) and how to handle multiple types of objects?
        public static void AddAndReplicateProvider(ContentProvider contentProvider, GameObject gameObject, object[] arguments)
        {
            gameObject.AddComponent(contentProvider.GetType());
            MyceliumNetwork.RPC(ContentPlugin.modID, nameof(ReplicateThinAirProvider), ReliableType.Reliable, (contentProvider.GetType().Name, arguments));
        }

        private static void ReplicateAddProvider(string contentProviderName, GameObject gameObject, object[] arguments)
        {
            
        }
        */
        #endregion
    }
}
