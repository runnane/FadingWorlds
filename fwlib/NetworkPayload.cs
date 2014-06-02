using System;
using System.Collections.Generic;
using ProtoBuf;

namespace fwlib
{
    public enum PayloadType
    {
        Unset,
        System,
        Auth,
        Message,
        Move,
        Data,
        Quit,
        Attack,
        PrivateMessage,
        User,
        UserList,
        EntityChange
    }

    public enum PayloadCommand
    {
        Unset,
        Ping,
        Pong,
        InitEntity,
        Helo,
        Login,
        Logout,
        GetMap,
        MapLoaded,
        GameLoaded,
        InitPlayer,
        Map,
        PleaseLoadGame,
        AuthPlease,
        Success,
        Fail,
        LoggedInDifferentLocation,
        OldVersion
    }

    [ProtoContract]
    public class NetworkPayload
    {

        [ProtoMember(1)]
        public PayloadType Type { get; set; }

        [ProtoMember(2)]
        public PayloadCommand Command { get; set; }

        [ProtoMember(3)]
        public List<String> Params { get; set; }

        [ProtoMember(4)]
        public bool Complete { get; set; }

        public NetworkPayload()
        {
            Type = PayloadType.Unset;
            Command = PayloadCommand.Unset;
            Complete = false;
            Params = new List<string>();
        }

        public override string ToString()
        {
            var s = String.Empty;
            //s += "Payload -";
            switch (Type)
            {
                case PayloadType.Unset:
                    s += " EMPTY payload!";
                    break;

                    case PayloadType.Move:
                    s += "[move] " + Params[0] + " -> " + Params[1] + "x" + Params[2];
                    break;
                default:
                    s += " [" + Type + "] ";
                    switch (Command)
                    {
                        case PayloadCommand.Unset:
                            break;

                        default:
                            s += " [" + Command + "] ";
                            break;
                    }
                    break;
            }
            return s;
        }
    }
}
