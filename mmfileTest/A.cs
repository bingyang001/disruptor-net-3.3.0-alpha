using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
namespace mmfileTest
{
[Serializable]
    [ProtoContract]
    public class A
    {        
        [ProtoMember(1,Name="a",IsRequired=false)]
        public string Name {get;set; }
        [ProtoMember(2,Name = "l", IsRequired = false)]
        public long l { get; set; }

        //protected A() { }
    }
}
