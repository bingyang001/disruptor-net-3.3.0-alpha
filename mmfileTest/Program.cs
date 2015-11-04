using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.IO;
using ProtoBuf.Meta;
using System.Runtime.Serialization.Formatters.Binary;
using Adaptive.SimpleBinaryEncoding;

namespace mmfileTest
{
    class ProtoBufFormat
    {
        [ThreadStatic]
        private static RuntimeTypeModel model;
        public static RuntimeTypeModel Model
        {
            get { return model ?? (model = TypeModel.Create()); }
        }
        public static RuntimeTypeModel CreateModel()
        {
            if (model == null)
            {
                model = TypeModel.Create();
            }
            //model.Add(typeof(MessagePersistent[]), false);
            //model.Compile();

            return model;
        }
        public static void Serialize(object dto, Stream outputStream)
        {
            Model.Serialize(outputStream, dto);
        }

        public static object Deserialize(Type type, Stream fromStream)
        {
            var obj = Model.Deserialize(fromStream, null, type);
            return obj;
        }

        public static void Serialize2<T>(T dto, Stream outputStream)
        {
         ProtoBuf.Serializer.Serialize<T>(outputStream,dto);
            //Model.Serialize(outputStream, dto);
        }

        public static T Deserialize2<T>(Type type, Stream fromStream)
        {
           return ProtoBuf.Serializer.Deserialize<T>(fromStream);
            //var obj = Model.Deserialize(fromStream, null, type);
            //return obj;
        }
    }

    public static class ProtoBufExtensions
    {
        public static byte[] ToProtoBuf<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                ProtoBufFormat.Serialize2(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromProtoBuf<T>(this byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var obj = (T)ProtoBufFormat.Deserialize2<T>(typeof(T), ms);
                return obj;
            }
        }

        public static T FromProtoBuf<T>(this Stream stream)
        {
            return (T)ProtoBufFormat.Deserialize2<T>(typeof(T), stream); ;
        }
    }
    class Program
    {
        static T ByteArrayToObject<T>(byte[] buffer)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();    // Create new BinaryFormatter
            MemoryStream memoryStream = new MemoryStream(buffer);       // Convert byte array to memory stream, set position to start
            return (T)binaryFormatter.Deserialize(memoryStream);           // Deserializes memory stream into an object and return
            //return buffer.FromProtoBuf<T>();
        }

        static byte[] ObjectToByteArray(object inputObject)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();    // Create new BinaryFormatter
            MemoryStream memoryStream = new MemoryStream();             // Create target memory stream
            binaryFormatter.Serialize(memoryStream, inputObject);       // Convert object to memory stream
            return memoryStream.ToArray();                              // Return memory stream as byte array
            //return inputObject.ToProtoBuf();
        }
        static void WriteObjectToMMF(string mmfFile, object objectData)
        {
            string mapName = "MyFile";
            
            // Convert .NET object to byte array
            byte[] buffer = ObjectToByteArray(objectData);

            DirectBuffer _directBuffer=new DirectBuffer (buffer);
            

            Console.WriteLine("size:"+buffer.Length);
            using (FileStream fs = new FileStream(mmfFile, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.SetLength(buffer.Length);
                // Create a new memory mapped file
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(
                fs
                , mapName
                , buffer.Length,
                    MemoryMappedFileAccess.ReadWrite
                    , new MemoryMappedFileSecurity() { }
                    , HandleInheritability.Inheritable
                    , true))
                {
                     //Create a view accessor into the file to accommmodate binary data size
                    using (MemoryMappedViewAccessor mmfWriter = mmf.CreateViewAccessor(0, buffer.Length))
                    {
                        // Write the data
                        mmfWriter.WriteArray<byte>(0, buffer, 0, buffer.Length);
                    }
                    //using (MemoryMappedViewStream mmfWriter = mmf.CreateViewStream(0, buffer.Length))
                    //{
                    //    // Write the data
                    //    mmfWriter.Write(buffer, 0, buffer.Length);
                    //}
                }
            }
        }
        static T ReadObjectFromMMF<T>(string mmfFile)
        {
            string mapName = "MyFile";
           
            using (FileStream fs = new FileStream(mmfFile, FileMode.Open, FileAccess.ReadWrite))
            {
                // Get a handle to an existing memory mapped file
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, mapName, fs.Length,
                    MemoryMappedFileAccess.ReadWrite, new MemoryMappedFileSecurity() { }, HandleInheritability.Inheritable, true))
                {
                    // Create a view accessor from which to read the data
                    using (MemoryMappedViewAccessor mmfReader = mmf.CreateViewAccessor())
                    {
                        // Create a data buffer and read entire MMF view into buffer
                        byte[] buffer = new byte[mmfReader.Capacity];
                        mmfReader.ReadArray<byte>(0, buffer, 0, buffer.Length);

                        // Convert the buffer to a .NET object
                        return ByteArrayToObject<T>(buffer);
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            try
            {
                //MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(
                //new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "a.message"), FileMode.OpenOrCreate, FileAccess.ReadWrite)
                //, "a"
                //, 0
                //, MemoryMappedFileAccess.ReadWrite
                //, new MemoryMappedFileSecurity()
                //, HandleInheritability.Inheritable
                //, false);
                //using (MemoryMappedViewAccessor fileMapView = memoryMappedFile.CreateViewAccessor())
                //{
                //    var by = new A { l = 100, Name = Guid.NewGuid().ToString() }.ToProtoBuf();


                //    fileMapView.WriteArray<byte>(0, by, 0, by.Length);

                //    byte[] buffer = new byte[fileMapView.Capacity];
                //    fileMapView.ReadArray<byte>(0, buffer, 0, buffer.Length);
                //    var a = buffer.FromProtoBuf<A>();


                //    Console.WriteLine(a.Name);
                //}
               // System.Net.Http.HttpClient
                for(var i=0;i<5;i++)
                { 
                WriteObjectToMMF(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "a.message"),new A { l = 100, Name = Guid.NewGuid().ToString() });
                var o= ReadObjectFromMMF<A>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "a.message"));
                Console.WriteLine (o.l+"__"+o.Name);
                }
                Console.Read();
            }
            catch  (Exception ex)
            {
               Console.WriteLine(ex.ToString ());
               Console.Read ();
            }
        }
    }
}
