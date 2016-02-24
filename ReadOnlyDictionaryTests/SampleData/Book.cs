using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadOnlyDictionaryTests.SampleData
{
    [Serializable]
    [ProtoContract]
    public class Book
    {
         [ProtoMember(1)]
        public string Name { get; set; }

         [ProtoMember(2)]
         public string Name2 { get; set; }

         [ProtoMember(3)]
         public string Name3 { get; set; }

        public Book(string name, string name2, string name3)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Empty name", "name");
            }

            this.Name = name;
            this.Name2 = name2;
            this.Name3 = name3;
        }

        public Book()
        {

        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var b2 = obj as Book;

            if (b2 != null)
            {
                return this.Name == b2.Name;
            }
            else
            {
                return base.Equals(b2);
            }
        }

        public override string ToString()
        {
            return String.Format("Book({0})", this.Name);
        }
    }
}
