#if SONARQUBE
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;

using SyntaxTree.VisualStudio.Unity.Bridge;

namespace Stratton.Core.Editor
{
    [InitializeOnLoad]
    public class ProjectFileHook
    {
        private const string _schema = @"http://schemas.microsoft.com/developer/msbuild/2003";
        private const string _assemblyName = "AssemblyName";//XML category for assembly
        private const string _assemblyCsharpRuntime = "Assembly-CSharp";//value for assembly name
        private const string _targetFrameworkVersion = "TargetFrameworkVersion";//XML category for .net framework
        private const string _frameworkExpected = "v4.7.1";//old value for framework
        private const string _frameworkDesired = "v4.7.2";//new value for framework

        class Utf8StringWriter : StringWriter
        {//to allow UTF8 saving
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }

        static ProjectFileHook()
        {
            ProjectFilesGenerator.ProjectFileGeneration += (string name, string content) =>
            {
                var document = XDocument.Parse(content);
                var assemblyName = document.Descendants(XName.Get(_assemblyName, _schema)).FirstOrDefault();
                if (null != assemblyName && assemblyName.Value.Contains(_assemblyCsharpRuntime))
                {
                    var target = document.Descendants(XName.Get(_targetFrameworkVersion, _schema)).FirstOrDefault();
                    if (null != target && target.Value.Contains(_frameworkExpected))
                    {
                        target.SetValue(_frameworkDesired);
                    }
                }
                var str = new Utf8StringWriter();
                document.Save(str);

                return str.ToString();
            };
        }
    }
}
#endif