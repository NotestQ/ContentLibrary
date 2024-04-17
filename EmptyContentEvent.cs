using System;
using Zorro.Core.Serizalization;

namespace ContentLibrary
{
    internal class EmptyContentEvent : ContentEvent
    {
        public EmptyContentEvent() 
        { 
        
        }

        public override void Deserialize(BinaryDeserializer deserializer)
        {
            throw new NotImplementedException();
        }

        public override Comment GenerateComment()
        {
            throw new NotImplementedException();
        }

        public override float GetContentValue()
        {
            throw new NotImplementedException();
        }

        public override ushort GetID()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            throw new NotImplementedException();
        }

        public override void Serialize(BinarySerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
