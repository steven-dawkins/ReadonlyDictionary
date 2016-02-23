using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadOnlyDictionaryTests.SampleData
{
    public class Book
    {
        public readonly string Name;

        public Book(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Empty name", "name");
            }

            this.Name = name;
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
