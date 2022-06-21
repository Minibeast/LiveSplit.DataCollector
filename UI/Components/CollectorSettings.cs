using System;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public partial class CollectorSettings : UserControl
    {

        public LayoutMode Mode { get; set; }

        public string Path { get; set; }
        public string URL { get; set; }

        public CollectorSettings()
        {
            InitializeComponent();

            Path = "Username";
            URL = "http://scanme.nmap.org";

            txtPath.DataBindings.Add("Text", this, "Path", false, DataSourceUpdateMode.OnPropertyChanged);
            txtURL.DataBindings.Add("Text", this, "URL", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;

            Version version = SettingsHelper.ParseVersion(element["Version"]);
            Path = SettingsHelper.ParseString(element["Path"]);
            URL = SettingsHelper.ParseString(element["URL"]);
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "1.0.0") ^
                SettingsHelper.CreateSetting(document, parent, "Path", Path) ^
                SettingsHelper.CreateSetting(document, parent, "URL", URL);
        }
    }
}
