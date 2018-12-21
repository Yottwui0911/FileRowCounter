using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileRowCounter.Model
{
    public class RowCounter
    {
        public RowCounter()
        {
            this.LoadConfig();
        }

        #region properties

        /// <summary>
        /// 対象のRootフォルダ
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// 指定する拡張子
        /// </summary>
        public string Extension { get; set; }

        private string m_processedExtenstion => string.IsNullOrWhiteSpace(this.Extension) ? "*.*" : $"*.{this.Extension}";

        /// <summary>
        /// ソートするかどうか
        /// </summary>
        public bool IsSort { get; set; }

        /// <summary>
        /// 上位何個まで表示するか
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// 対象外のフォルダ
        /// </summary>
        public IEnumerable<string> ExceptDirectory { get; set; }

        #endregion properties

        #region methods

        /// <summary>
        /// ファイルパスとファイルの行数のDictionaryを返す
        /// </summary>
        /// <returns></returns>
        public async Task<IDictionary<string, int>> CalcRowCount()
        {
            // Key=ファイルパス, Value=ファイルの行数のDictionaryを作る
            var dic = new Dictionary<string, int>();

            await Task.Run(() => this.Getfiles(this.FolderPath, ref dic));

            if (this.IsSort)
            {
                dic = this.SortDictionary(dic);
            }

            // 最後に検索した設定を保存
            this.SaveConfig();

            return dic;
        }

        /// <summary>
        /// Rootフォルダ配下のファイルの行数を走査する
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="dic"></param>
        private void Getfiles(string rootPath, ref Dictionary<string, int> dic)
        {
            foreach (var f in Directory.GetFiles(rootPath, this.m_processedExtenstion))
            {
                var filePath = Path.Combine(rootPath, f);
                dic.Add(filePath, File.ReadAllLines(filePath).Length);
            }

            var directories = Directory.GetDirectories(rootPath);

            // 空文字以外の無視リストが存在する場合、そのディレクトリだけ除く
            if(this.ExceptDirectory.Any(x=>!string.IsNullOrWhiteSpace(x)))
            {
                directories = directories.Where(x => !this.Contains(x)).ToArray();
            }

            foreach (var d in directories)
            {
                // 再帰的に関数を呼び出すことですべてのフォルダのファイルを走査する
                this.Getfiles(d, ref dic);
            }
        }

        /// <summary>
        /// 対象のRootファイルに無視するフォルダの文字列が存在するかどうか
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private bool Contains(string source)
        {
            return this.ExceptDirectory.Any(x => source.Contains(x));
        }

        /// <summary>
        /// 行数のランキング作成
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        private Dictionary<string, int> SortDictionary(IDictionary<string, int> dic)
        {
            var sortedDic = dic.OrderByDescending(x => x.Value).Take(this.MaxCount);
            return sortedDic.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// configファイルPath
        /// </summary>
        private static readonly string m_config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "row_counter.config");

        private static readonly char m_exceptDirectorySeparater = ',';

        private static readonly char m_configSeparater = '\t';

        /// <summary>
        /// Configファイルに設定を保存しておく
        /// </summary>
        private void SaveConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {nameof(this.FolderPath),this.FolderPath },
                {nameof(this.Extension),this.Extension },
                {nameof(this.IsSort),this.IsSort.ToString() },
                {nameof(this.MaxCount),this.MaxCount.ToString() },
                {nameof(this.ExceptDirectory), string.Join(m_exceptDirectorySeparater.ToString() ,this.ExceptDirectory) },
            };

            using (var sw = new StreamWriter(m_config, false))
            {
                foreach (var item in dic)
                {
                    sw.WriteLine($"{item.Key}{m_configSeparater}{item.Value}");
                }
            }
        }

        /// <summary>
        /// Configファイルから設定を読み込む
        /// </summary>
        private void LoadConfig()
        {
            if(!File.Exists(m_config))
            {
                return;
            }

            using (var sr = new StreamReader(m_config))
            {
                var lines = new Dictionary<string, string>();

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine().Split(m_configSeparater);

                    // KeyValuePairにならない場合無視
                    if (line.Count() < 2)
                    {
                        continue;
                    }
                    lines.Add(line[0], line[1]);
                }

                // 読み込んだConfigの結果を反映
                // TODO:2018-12-21 t-yoshizumi もっといい復元ロジックがあればそちらを採用
                foreach (var line in lines)
                {
                    if (line.Key == nameof(this.FolderPath))
                    {
                        this.FolderPath = line.Value;
                    }
                    if (line.Key == nameof(this.Extension))
                    {
                        this.Extension = line.Value;
                    }
                    if (line.Key == nameof(this.IsSort) && bool.TryParse(line.Value, out bool isSort))
                    {
                        this.IsSort = isSort;
                    }
                    if (line.Key == nameof(this.MaxCount) && int.TryParse(line.Value, out int maxCount))
                    {
                        this.MaxCount = maxCount;
                    }
                    if (line.Key == nameof(this.ExceptDirectory))
                    {
                        this.ExceptDirectory = line.Value.Split(m_exceptDirectorySeparater);
                    }
                }
            }
        }

        #endregion methods
    }
}
