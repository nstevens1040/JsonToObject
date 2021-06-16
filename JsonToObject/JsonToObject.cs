// Decompiled with JetBrains decompiler
// Type: Microsoft.PowerShell.Commands.JsonObject
// Assembly: Microsoft.PowerShell.Commands.Utility, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: 7BE50D38-9B0A-4082-9265-05888B22349F
// Assembly location: C:\Users\nstevens\Desktop\Microsoft.PowerShell.Commands.Utility.dll

using JsonToObject.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Web.Script.Serialization;
using System.Reflection;

namespace Deserialize
{
    internal class JsonObjectTypeResolver : JavaScriptTypeResolver
    {
        public override Type ResolveType(string id) => typeof(Dictionary<string, object>);

        public override string ResolveTypeId(Type type) => string.Empty;
    }
    public class JsonObject
    {
        public JsonObject()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
        }
        private const int maxDepthAllowed = 1000;
        public object ConvertFromJson(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            ErrorRecord error = (ErrorRecord)null;
            object obj = new JavaScriptSerializer((JavaScriptTypeResolver)new JsonObjectTypeResolver())
            {
                RecursionLimit = 1020,
                MaxJsonLength = int.MaxValue
            }.DeserializeObject(input);
            switch (obj)
            {
                case IDictionary<string, object> _:
                    obj = (object)JsonObject.PopulateFromDictionary(obj as IDictionary<string, object>, out error);
                    break;
                case ICollection<object> _:
                    obj = (object)JsonObject.PopulateFromList(obj as ICollection<object>, out error);
                    break;
            }
            return obj;
        }
        private static ICollection<object> PopulateFromList(
            ICollection<object> list,
            out ErrorRecord error
        )
        {
            error = (ErrorRecord)null;
            List<object> objectList = new List<object>();
            foreach (object obj in (IEnumerable<object>)list)
            {
                switch (obj)
                {
                    case IDictionary<string, object> _:
                        PSObject psObject = JsonObject.PopulateFromDictionary(obj as IDictionary<string, object>, out error);
                        if (error != null)
                        {
                            return (ICollection<object>)null;
                        }
                        objectList.Add((object)psObject);
                        continue;
                    case ICollection<object> _:
                        ICollection<object> objects = JsonObject.PopulateFromList(obj as ICollection<object>, out error);
                        if (error != null)
                        {
                            return (ICollection<object>)null;
                        }
                        objectList.Add((object)objects);
                        continue;
                    default:
                        objectList.Add(obj);
                        continue;
                }
            }
            return (ICollection<object>)objectList.ToArray();
        }
        private static PSObject PopulateFromDictionary(
            IDictionary<string, object> entries,
            out ErrorRecord error
        )
        {
            error = (ErrorRecord)null;
            PSObject psObject1 = new PSObject();
            foreach (KeyValuePair<string, object> entry in (IEnumerable<KeyValuePair<string, object>>)entries)
            {
                PSPropertyInfo property = psObject1.Properties[entry.Key];
                if (property != null)
                {
                    string message = string.Format((IFormatProvider)CultureInfo.InvariantCulture, webCmdletStrings.DuplicateKeysInJsonString, (object)property.Name, (object)entry.Key);
                    error = new ErrorRecord((Exception)new InvalidOperationException(message), "DuplicateKeysInJsonString", ErrorCategory.InvalidOperation, (object)null);
                    return (PSObject)null;
                }
                if (entry.Value is IDictionary<string, object>)
                {
                    PSObject psObject2 = JsonObject.PopulateFromDictionary(entry.Value as IDictionary<string, object>, out error);
                    if (error != null)
                        return (PSObject)null;
                    psObject1.Properties.Add((PSPropertyInfo)new PSNoteProperty(entry.Key, (object)psObject2));
                }
                else if (entry.Value is ICollection<object>)
                {
                    ICollection<object> objects = JsonObject.PopulateFromList(entry.Value as ICollection<object>, out error);
                    if (error != null)
                        return (PSObject)null;
                    psObject1.Properties.Add((PSPropertyInfo)new PSNoteProperty(entry.Key, (object)objects));
                }
                else
                {
                    psObject1.Properties.Add((PSPropertyInfo)new PSNoteProperty(entry.Key, entry.Value));
                }
            }
            return psObject1;
        }
    }
    public class Json
    {
        public static object Convert(string inputJson)
        {
            JsonObject jo = new JsonObject();
            object deserialized = jo.ConvertFromJson(inputJson);
            return deserialized;
        }
    }
}
