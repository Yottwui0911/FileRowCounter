using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileRowCounter.Model
{
    public class RowCounter
    {
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

            foreach (var d in Directory.GetDirectories(rootPath))
            {
                // 再帰的に関数を呼び出すことですべてのフォルダのファイルを走査する
                this.Getfiles(d, ref dic);
            }
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
    }
}
