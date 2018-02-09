using System;
using System.Drawing;
using System.IO;
using System.Xml;
using Intersect.Enums;
using Mono.Data.Sqlite;

namespace Intersect.Editor.Classes
{
    public static class Database
    {
        private const string DB_FILENAME = "resources/mapcache.db";

        //Map Table Constants
        private const string MAP_CACHE_TABLE = "mapcache";

        private const string MAP_CACHE_ID = "id";
        private const string MAP_CACHE_REVISION = "revision";
        private const string MAP_CACHE_DATA = "data";

        //Options Constants
        private const string OPTION_TABLE = "options";

        private const string OPTION_ID = "id";
        private const string OPTION_NAME = "name";
        private const string OPTION_VALUE = "value";
        private static SqliteConnection sDbConnection;

        //Grid Variables
        public static bool GridHideDarkness;

        public static bool GridHideFog;
        public static bool GridHideOverlay;
        public static bool GridHideResources;
        public static int GridLightColor = System.Drawing.Color.White.ToArgb();

        //Options File
        public static bool LoadOptions()
        {
            if (!Directory.Exists("resources")) Directory.CreateDirectory("resources");
            if (!File.Exists("resources/config.xml"))
            {
                var settings = new XmlWriterSettings {Indent = true};
                using (var writer = XmlWriter.Create("resources/config.xml", settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteComment("Config.xml generated automatically by Intersect Game Engine.");
                    writer.WriteStartElement("Config");
                    writer.WriteElementString("Language", "English");
                    writer.WriteElementString("Host", "localhost");
                    writer.WriteElementString("Port", "5400");
                    writer.WriteElementString("RenderCache", "true");
                    //Not used by the editor, but created here just in case we ever want to share a resource folder with a client.
                    writer.WriteElementString("MenuBGM", "");
                    //Not used by the editor, but created here just in case we ever want to share a resource folder with a client.
                    writer.WriteElementString("MenuBG", "background.png");
                    //Not used by the editor, but created here just in case we ever want to share a resource folder with a client.
                    writer.WriteElementString("Logo", "logo.png");
                    //Not used by the editor, but created here just in case we ever want to share a resource folder with a client.
                    writer.WriteElementString("IntroBG", "");
                    //Not used by the editor, but created here just in case we ever want to share a resource folder with a client.
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                }
            }
            else
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(File.ReadAllText("resources/config.xml"));
                    Options.Language = "English";
                    if (xmlDoc.SelectSingleNode("//Config/Language") != null)
                    {
                        Options.Language = xmlDoc.SelectSingleNode("//Config/Language").InnerText;
                    }
                    Globals.ServerHost = xmlDoc.SelectSingleNode("//Config/Host").InnerText;
                    Globals.ServerPort = int.Parse(xmlDoc.SelectSingleNode("//Config/Port").InnerText);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        //Game Object Handling
        public static string[] GetGameObjectList(GameObjectType type) => type.GetLookup()?.Names;

        public static int GameObjectIdFromList(GameObjectType type, int listIndex) => listIndex < 0
            ? -1
            : (type.GetLookup()?.ValueList?[listIndex]?.Index ?? -1);

        public static int GameObjectListIndex(GameObjectType type, int id)
        {
            var index = type.GetLookup()?.IndexList?.IndexOf(id);
            if (!index.HasValue) throw new ArgumentNullException();
            return index.Value;
        }

        //Map Cache DB
        public static void InitMapCache()
        {
            if (!Directory.Exists("resources")) Directory.CreateDirectory("resources");
            SqliteConnection.SetConfig(SQLiteConfig.Serialized);
            if (!File.Exists(DB_FILENAME)) CreateDatabase();
            if (sDbConnection == null)
            {
                sDbConnection = new SqliteConnection("Data Source=" + DB_FILENAME + ",Version=3");
                sDbConnection.Open();
            }
            GridHideDarkness = GetOptionBool("HideDarkness");
            GridHideFog = GetOptionBool("HideFog");
            GridHideOverlay = GetOptionBool("HideOverlay");
            GridLightColor = GetOptionInt("LightColor");
            GridHideResources = GetOptionBool("HideResources");
        }

        private static void CreateDatabase()
        {
            sDbConnection = new SqliteConnection("Data Source=" + DB_FILENAME + ",Version=3,New=True");
            sDbConnection.Open();
            CreateOptionsTable();
            CreateMapCacheTable();
        }

        public static void CreateOptionsTable()
        {
            var cmd = "CREATE TABLE " + OPTION_TABLE + " ("
                      + OPTION_ID + " INTEGER PRIMARY KEY,"
                      + OPTION_NAME + " TEXT UNIQUE,"
                      + OPTION_VALUE + " TEXT"
                      + ");";
            using (var createCommand = sDbConnection.CreateCommand())
            {
                createCommand.CommandText = cmd;
                createCommand.ExecuteNonQuery();
            }
        }

        public static void SaveGridOptions()
        {
            SaveOption("HideDarkness", GridHideDarkness.ToString());
            SaveOption("HideFog", GridHideFog.ToString());
            SaveOption("HideOverlay", GridHideOverlay.ToString());
            SaveOption("HideResources", GridHideResources.ToString());
            SaveOption("LightColor", GridLightColor.ToString());
        }

        public static void SaveOption(string name, string value)
        {
            var query = "INSERT OR REPLACE into " + OPTION_TABLE + " (" + OPTION_NAME + "," +
                        OPTION_VALUE + ")" + " VALUES " + " (@" +
                        OPTION_NAME + ",@" + OPTION_VALUE + ");";
            using (SqliteCommand cmd = new SqliteCommand(query, sDbConnection))
            {
                cmd.Parameters.Add(new SqliteParameter("@" + OPTION_NAME, name));
                cmd.Parameters.Add(new SqliteParameter("@" + OPTION_VALUE, value));
                cmd.ExecuteNonQuery();
            }
        }

        public static string GetOptionStr(string name)
        {
            var query = "SELECT * from " + OPTION_TABLE + " WHERE " + OPTION_NAME + "=@" + OPTION_NAME + ";";
            using (SqliteCommand cmd = new SqliteCommand(query, sDbConnection))
            {
                cmd.Parameters.Add(new SqliteParameter("@" + OPTION_NAME, name));
                var dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    if (dataReader[OPTION_VALUE].GetType() != typeof(DBNull))
                    {
                        var data = (string) dataReader[OPTION_VALUE];
                        return data;
                    }
                }
            }
            return "";
        }

        public static int GetOptionInt(string name)
        {
            var opt = GetOptionStr(name);
            if (opt != "")
            {
                return Convert.ToInt32(opt);
            }
            return -1;
        }

        public static bool GetOptionBool(string name)
        {
            var opt = GetOptionStr(name);
            if (opt != "")
            {
                return Convert.ToBoolean(opt);
            }
            return false;
        }

        public static void CreateMapCacheTable()
        {
            var cmd = "CREATE TABLE " + MAP_CACHE_TABLE + " ("
                      + MAP_CACHE_ID + " INTEGER PRIMARY KEY,"
                      + MAP_CACHE_REVISION + " INTEGER,"
                      + MAP_CACHE_DATA + " BLOB"
                      + ");";
            using (var createCommand = sDbConnection.CreateCommand())
            {
                createCommand.CommandText = cmd;
                createCommand.ExecuteNonQuery();
            }
        }

        public static int[] LoadMapCache(int id, int revision, int w, int h)
        {
            var data = LoadMapCacheRaw(id, revision);
            if (data != null)
            {
                using (var ms = new MemoryStream(data))
                {
                    var bmp = new Bitmap(Image.FromStream(ms), w, h);
                    //Gonna do really sketchy probably broken math here -- yolo
                    int[] imgData = new int[bmp.Width * bmp.Height];

                    unsafe
                    {
                        // lock bitmap
                        System.Drawing.Imaging.BitmapData origdata =
                            bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);

                        uint* byteData = (uint*) origdata.Scan0;

                        // Switch bgra -> rgba
                        for (int i = 0; i < imgData.Length; i++)
                        {
                            byteData[i] = (byteData[i] & 0x000000ff) << 16 | (byteData[i] & 0x0000FF00) |
                                          (byteData[i] & 0x00FF0000) >> 16 | (byteData[i] & 0xFF000000);
                        }

                        // copy data
                        System.Runtime.InteropServices.Marshal.Copy(origdata.Scan0, imgData, 0, bmp.Width * bmp.Height);

                        byteData = null;

                        // unlock bitmap
                        bmp.UnlockBits(origdata);
                    }

                    byte[] result = new byte[imgData.Length * sizeof(int)];
                    Buffer.BlockCopy(imgData, 0, result, 0, result.Length);
                    return imgData;
                }
            }
            return null;
        }

        public static Image LoadMapCacheLegacy(int id, int revision)
        {
            var data = LoadMapCacheRaw(id, revision);
            if (data != null)
            {
                using (var ms = new MemoryStream(data))
                {
                    return Image.FromStream(ms);
                }
            }
            return null;
        }

        public static byte[] LoadMapCacheRaw(int id, int revision)
        {
            var query = "SELECT * from " + MAP_CACHE_TABLE + " WHERE " + MAP_CACHE_ID + "=@" + MAP_CACHE_ID;
            if (revision > -1)
            {
                query += " AND " + MAP_CACHE_REVISION + "=@" + MAP_CACHE_REVISION;
            }

            using (SqliteCommand cmd = new SqliteCommand(query, sDbConnection))
            {
                cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_ID, id.ToString()));
                if (revision > -1)
                {
                    cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_REVISION, revision.ToString()));
                }
                var dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    if (dataReader[MAP_CACHE_DATA].GetType() != typeof(DBNull))
                    {
                        var data = (byte[]) dataReader[MAP_CACHE_DATA];
                        return data;
                    }
                }
            }
            return null;
        }

        public static void SaveMapCache(int id, int revision, byte[] data)
        {
            var query = "INSERT OR REPLACE into " + MAP_CACHE_TABLE + " (" + MAP_CACHE_ID + "," +
                        MAP_CACHE_REVISION + "," + MAP_CACHE_DATA + ")" + " VALUES " + " (@" +
                        MAP_CACHE_ID + ",@" + MAP_CACHE_REVISION + ",@" + MAP_CACHE_DATA + ");";
            using (SqliteCommand cmd = new SqliteCommand(query, sDbConnection))
            {
                cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_ID, id));
                cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_REVISION, revision));
                if (data != null)
                {
                    cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_DATA, data));
                }
                else
                {
                    cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_DATA, null));
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearMapCache(int id)
        {
            var query = "UPDATE " + MAP_CACHE_TABLE + " SET " + MAP_CACHE_DATA + " = @" + MAP_CACHE_DATA + " WHERE " +
                        MAP_CACHE_ID + " = @" + MAP_CACHE_ID;
            using (SqliteCommand cmd = new SqliteCommand(query, sDbConnection))
            {
                cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_ID, id));
                cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_DATA, null));
                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearAllMapCache()
        {
            var query = "UPDATE " + MAP_CACHE_TABLE + " SET " + MAP_CACHE_DATA + " = @" + MAP_CACHE_DATA;
            using (SqliteCommand cmd = new SqliteCommand(query, sDbConnection))
            {
                cmd.Parameters.Add(new SqliteParameter("@" + MAP_CACHE_DATA, null));
                cmd.ExecuteNonQuery();
            }
        }
    }
}