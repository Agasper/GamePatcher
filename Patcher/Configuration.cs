using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace Patcher
{
    class Configuration
    {
        public string NewsUrl
        {
            get
            {
                return newsUrl;
            }
        }
        public string GameName
        {
            get
            {
                return gameName;
            }
        }
        public string PublisherUrl
        {
            get
            {
                return publisherUrl;
            }
        }
        public string GameUrl
        {
            get
            {
                return gameUrl;
            }
        }
        public string GameExe
        {
            get
            {
                return gameExe;
            }
        }
        public string PatchesDirectory
        {
            get
            {
                return patchesDirectory;
            }
        }
        public string VersionUrl
        {
            get
            {
                return versionUrl;
            }
        }

        XmlDocument xml;
        string newsUrl;
        string gameName;
        string publisherUrl;
        string gameUrl;
        string gameExe;
        string patchesDirectory;
        string versionUrl;

        public Configuration(string path)
        {
            xml = new XmlDocument();
            xml.Load(path);

            try
            {
                versionUrl = xml["root"]["check_version_url"].InnerText;
                patchesDirectory = xml["root"]["patches_directory"].InnerText;
                gameExe = xml["root"]["game_exe"].InnerText;
                gameUrl = xml["root"]["game_url"].InnerText;
                publisherUrl = xml["root"]["publisher_url"].InnerText;
                gameName = xml["root"]["game_name"].InnerText;
                newsUrl = xml["root"]["news_url"].InnerText;
            }
            catch
            {
                throw new Exception("configuration.xml is wrong formatted. Right is:\n\n<?xml version=\"1.0\"?>\n<root>\n    <game_name>TestGame</game_name>\n    <game_exe>Test.exe</game_exe>\n    <game_url>URL Opens when user click to game logo</game_url>\n    <check_version_url>http://gamesite.com/getLastVersionNumber </check_version_url>\n    <patches_directory>http://gamesite.com/patches/ </patches_directory>\n    <news_url>News loading from here</news_url>\n    <publisher_url>URL Opens when user click to game logo</publisher_url>\n</root>");
            }
        }
    }
}
