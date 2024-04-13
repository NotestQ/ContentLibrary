using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MyceliumNetworking;
using System;
using Zorro.Core.Serizalization;
using Unity.Collections;
using System.Text;
using Steamworks;

namespace ContentLibrary
{
    [BepInDependency("RugbugRedfern.MyceliumNetworking", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ContentPlugin : BaseUnityPlugin
    {
        public static ContentPlugin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        internal static ConfigEntry<bool>? ConfigDebugMode { get; private set; }
        internal const uint modID = 1215985525;
        internal bool DebugMode = false;
        internal static bool DebugState;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            Patch();
            ConfigDebugMode = Config.Bind("General", "Debug", false,
            "If the library should log debug messages.");
            #if DEBUG
                DebugMode = true;
            #endif
            DebugState = DebugMode || ConfigDebugMode.Value;

            MyceliumNetwork.RegisterNetworkObject(this, modID);
            MyceliumNetwork.LobbyEntered += ContentLibrary.OnLobbyEntered;

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        #region Extras

        public static void RPCTargetRelay(string methodName, CSteamID steamID, string contentProviderName, params object[] args)
        {
            var serializer = new TypeSerializer();
            string deserializationInfo = ""; // This is dumb, right?
            var infoSerializer = new BinarySerializer();

            for (int i = 0; i < args.Length; i++)
            {
                string type = args[i].GetType().Name;
                switch (type)
                {
                    case "Byte":
                        deserializationInfo += "a";
                        serializer.WriteByte((byte)args[i]);
                        break;
                    case "Boolean":
                        deserializationInfo += "b";
                        serializer.WriteBool((bool)args[i]);
                        break;
                    case "Int16":
                        deserializationInfo += "c";
                        serializer.WriteShort((short)args[i]);
                        break;
                    case "Int32":
                        deserializationInfo += "d";
                        serializer.WriteInt((int)args[i]);
                        break;
                    case "Int64":
                        deserializationInfo += "e";
                        serializer.WriteLong((long)args[i]);
                        break;
                    case "UInt16":
                        deserializationInfo += "f";
                        serializer.WriteUshort((ushort)args[i]);
                        break;
                    case "UInt32":
                        deserializationInfo += "g";
                        serializer.WriteUInt((ushort)args[i]);
                        break;
                    case "UInt64":
                        deserializationInfo += "h";
                        serializer.WriteUlong((ulong)args[i]);
                        break;
                    case "String":
                        deserializationInfo += "i";
                        serializer.WriteString((string)args[i], Encoding.UTF8);
                        break;
                    case "Vector3":
                        deserializationInfo += "j";
                        serializer.WriteFloat3((float)args[i]);
                        break;
                    case "Quaternion":
                        deserializationInfo += "k";
                        serializer.WriteFloat4((float)args[i]);
                        break;
                    default:
                        throw new Exception($"Type {type} not currently supported");
                }
            }

            byte[] byteArray = [];
            var buffer = serializer.buffer;
            ByteArrayConvertion.MoveToByteArray(ref buffer, ref byteArray);

            infoSerializer.WriteString(deserializationInfo, Encoding.UTF8);
            byte[] infoByteArray = [];
            var infoBuffer = infoSerializer.buffer;
            ByteArrayConvertion.MoveToByteArray(ref infoBuffer, ref infoByteArray);

            CLogger.LogDebug($"Buffer type is {serializer.buffer.GetType()}, {byteArray.GetType()}");
            MyceliumNetwork.RPCTarget(modID, methodName, steamID, ReliableType.Reliable, contentProviderName, infoByteArray, byteArray);
        }

        /*
         * I think this is horrible yeah, feel free to ask why and make a pull request
         * Would be way better as a type to method dict but I couldn't figure that out as obvious it may sound
         * But what is a switch case if not a big dict am I right... (no)
         */
        [CustomRPC] 
        private void ReplicatePollProvider(string contentProviderName, byte[] infoByteArray, byte[] byteArguments)
        { 
            var infoDeserializer = new BinaryDeserializer(infoByteArray, Allocator.Persistent);
            var argumentDeserializer = new BinaryDeserializer(byteArguments, Allocator.Persistent);

            string deserializationInfo = infoDeserializer.ReadString(Encoding.UTF8);
            var arguments = new object[deserializationInfo.Length];

            CLogger.LogDebug($"Received {contentProviderName} with deserialization info \"{deserializationInfo}\"");
            int iteration = 0;
            foreach (char character in deserializationInfo)
            {
                switch (character.ToString())
                {
                    case "a": // Byte
                        arguments[iteration] = argumentDeserializer.ReadByte();
                        break;
                    case "b": // Boolean
                        arguments[iteration] = argumentDeserializer.ReadBool();
                        break;
                    case "c": // Int16
                        NativeSlice<byte> nativeSlice = new NativeSlice<byte>(argumentDeserializer.buffer, argumentDeserializer.position, 2);
                        short result = nativeSlice.SliceConvert<short>()[0];
                        argumentDeserializer.position += 2; // Got lazy sorry
                        arguments[iteration] = result;
                        break;
                    case "d": // Int32
                        arguments[iteration] = argumentDeserializer.ReadInt();
                        break;
                    case "e": // Int64
                        arguments[iteration] = argumentDeserializer.ReadLong();
                        break;
                    case "f": // UInt16
                        arguments[iteration] = argumentDeserializer.ReadUShort();
                        break;
                    case "g": // UInt32
                        arguments[iteration] = argumentDeserializer.ReadUInt();
                        break;
                    case "h": // UInt64
                        arguments[iteration] = argumentDeserializer.ReadUlong();
                        break;
                    case "i": // String
                        arguments[iteration] = argumentDeserializer.ReadString(Encoding.UTF8);
                        break;
                    case "j": // Vector3
                        arguments[iteration] = argumentDeserializer.ReadFloat3();
                        break;
                    case "k": // Quaternion
                        arguments[iteration] = argumentDeserializer.ReadFloat4();
                        break;
                    default: // None
                        throw new Exception($"Unknown deserialization character {character}");
                }
                iteration++;
            }

            ContentProvider contentProvider = ContentLibrary.GetContentProviderFromName(contentProviderName);
            var componentInParent = (ContentProvider)Activator.CreateInstance(contentProvider.GetType(), arguments);
            ContentPolling.contentProviders.Add(componentInParent, 1);
        }

        #endregion

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
