using System;
using System.Collections.Generic;
using System.Linq;
using MyceliumNetworking;
using Photon.Pun;
using Steamworks;

namespace ContentLibrary
{
    public static class ContentHandler
    {
        #region Main

        public static List<ContentEvent>? EventList;
        // Holds what events should be assigned or synced until you enter a lobby
        public static List<ContentEvent> TemporaryEventList = new List<ContentEvent>();

        /// <summary>
        /// If player is the host this method assings events IDs and syncs them with the lobby data
        /// Otherwise, this method gets the events IDs from the lobby data
        /// </summary>
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
                    ushort id = (ushort)(1999 + EventList.Count);
                    CLogger.LogDebug($"Added ContentEvent index {index}, type name {contentEvent.GetType().Name}, id {id}");
                    MyceliumNetwork.SetLobbyData("ContentLibrary_" + contentEvent.GetType().Name, id);
                }
                return;
            }

            for (var index = 0; index < TemporaryEventList.Count; index++) // This is scuffed. Sorry.
            {
                EventList.Add(new EmptyContentEvent());
            }

            for (var index = 0; index < TemporaryEventList.Count; index++)
            {
                ContentEvent contentEvent = TemporaryEventList[index];
                ushort id = MyceliumNetwork.GetLobbyData<ushort>("ContentLibrary_" + contentEvent.GetType().Name);
                CLogger.LogDebug($"Added ContentEvent, TemporaryEventList index {index}, type name {contentEvent.GetType().Name}, id {id}");
                EventList[id - 2000] = contentEvent;
            }
        }

        /// <summary>
        /// Call this at your plugin's Awake() if you wish to use the library's ID management.
        /// Assigns an ID to your ContentEvent automatically.
        /// </summary>
        /// <param name="contentEvent"></param>
        public static void AssignEvent(ContentEvent contentEvent)
        {
            // We probably don't need this if every mod assigns an event at the same time, but this is just a guarantee
            MyceliumNetwork.RegisterLobbyDataKey("ContentLibrary_" + contentEvent.GetType().Name);
            // Holds the contentEvents in any order, unsynced
            TemporaryEventList.Add(contentEvent);
        }

        /// <summary>
        /// On your ContentEvent's GetID() method you need to return GetEventID(NameOfYourContentEvent)
        /// </summary>
        /// <param name="contentEventName"></param>
        /// <returns>Returns your event's automatically assigned ID for you.</returns>
        public static ushort GetEventID(string contentEventName)
        {       
            int foundIndex = EventList!.FindIndex(match => match.GetType().Name == contentEventName);
            if (foundIndex == -1) 
            {
                for (var index = 0; index < EventList!.Count; index++)
                {
                    CLogger.LogDebug($"{EventList[index].GetType().Name}, {contentEventName}, {EventList[index].GetType().Name == contentEventName}");
                }
                CLogger.LogError($"GetEventID for {contentEventName} returned -1");
            }

            return (ushort)(2000 + foundIndex);
        }

        /// <summary>
        /// Gets the player holding a camera.
        /// </summary>
        /// <returns>Returns a Photon.Realtime.Player if a player holding a camera is found.</returns>
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

        /// <summary>
        /// Polls manually making it easier to add the same event multiple times to the contentEvents list.
        /// Exists because the amount of content events in the list matters, with at least 100 (representing 100 frames) to get the full score.
        /// </summary>
        /// <param name="contentProvider"></param>
        /// <param name="screenCoverage"></param>
        /// <param name="timesToPoll"></param>
        public static void ManualPoll(ContentProvider contentProvider, float screenCoverage = 1, int timesToPoll = 1)
        {
            if (ContentPolling.m_currentPollingCamera == null)
            {
                CLogger.LogDebug("ManualPoll could not carry out, camera is currently not polling.");
                return;
            }

            for (int i = 0; i < timesToPoll; i++)
            {
                contentProvider.GetContent(ContentPolling.contentEvents, screenCoverage, ContentPolling.m_currentPollingCamera, ContentPolling.m_clipTime);
            }
        }

        public static void ManualPoll(ContentProvider contentProvider, int screenCoverage = 400, int timesToPoll = 1)
        {
            if (ContentPolling.m_currentPollingCamera == null)
            {
                CLogger.LogDebug("ManualPoll could not carry out, camera is currently not polling.");
                return;
            }

            float seenAmount = (float)screenCoverage / 400f;

            for (int i = 0; i < timesToPoll; i++)
            {
                contentProvider.GetContent(ContentPolling.contentEvents, seenAmount, ContentPolling.m_currentPollingCamera, ContentPolling.m_clipTime);
            }
        }

        #endregion

        #region Extras

        public static List<ContentProvider> ProviderList = new List<ContentProvider>();

        /// <summary>
        /// At your plugin's awake, you need to assign each of your ContentProviders through the library if you wish to use the extra features
        /// such as handle provider construction and replication.
        /// </summary>
        /// <param name="contentProvider"></param>
        public static void AssignProvider(ContentProvider contentProvider)
        {
            ProviderList.Add(contentProvider);
        }

        /// <summary>
        /// Gets a ContentProvider from the assigned provider list.
        /// </summary>
        /// <param name="contentProviderName"></param>
        /// <returns>Returns a ContentProvider from their class name.</returns>
        public static ContentProvider GetContentProviderFromName(string contentProviderName)
        {
            return ProviderList.Find(match => match.GetType().Name == contentProviderName);
        }

        /// <summary>
        /// This handles polling a provider and replicating it for you, it makes heavy use of the Activator() and writes your arguments
        /// to a buffer so it can go through an RPC. *This CAN be done better if you do it yourself* as you'd know exactly what
        /// arguments you want to pass to your provider, however if you don't really care this is perfect for you!
        /// </summary>
        /// <param name="contentProvider"></param>
        /// <param name="screenCoverage"></param>
        /// <param name="arguments"></param>
        public static void PollAndReplicateProvider(ContentProvider contentProvider, int screenCoverage = 400, int timesToPoll = 1, params object[] arguments)
        {
            // Maybe throw exception if we can't GetContentProviderFromName?
            // From prior testing I'm sure only the camera man needs to create the provider
            var player = GetPlayerWithCamera();

            if (player == null) return;

            var componentInParent = (ContentProvider)Activator.CreateInstance(contentProvider.GetType(), arguments);
            
            if (player.IsLocal)
            {
                CLogger.LogDebug("Local player is the one holding the camera, creating provider...");

                ManualPoll(contentProvider, screenCoverage, timesToPoll);
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

                ContentPlugin.RPCTargetRelay("ReplicatePollProvider", steamID, contentProvider.GetType().Name, screenCoverage, timesToPoll, arguments);
                // Just to make sure, we still poll a provider, yes this is dumb but I have yet to test it without this
                ManualPoll(contentProvider, screenCoverage, timesToPoll);
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
