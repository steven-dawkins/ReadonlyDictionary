namespace ReadonlyDictionary.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface ISerializer<T>
    {
        byte[] Serialize(T value);

        T Deserialize(byte[] bytes);

        Object GetState();
    }
}
