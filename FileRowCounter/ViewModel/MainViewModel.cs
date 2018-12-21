using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using FileRowCounter.Model;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;

namespace FileRowCounter.ViewModel
{
    public class MainViewModel : BindableBase
    {
        public MainViewModel()
        {
            this.ShowSearchDirectotyCommand = new DelegateCommand(this.ShowSerchDirectory);
            this.ExecuteCommand = new DelegateCommand(async () => await this.Execute(), this.CanExcecute);
        }

        private readonly RowCounter m_fileRowCounter = new RowCounter();

        /// <summary>
        /// 対象のRootフォルダ
        /// </summary>
        public string FolderPath
        {
            get { return this.m_fileRowCounter.FolderPath; }
            set
            {
                if (this.m_fileRowCounter.FolderPath == value)
                {
                    return;
                }

                this.m_fileRowCounter.FolderPath = value;
                this.RaisePropertyChanged(nameof(this.FolderPath));
            }
        }

        /// <summary>
        /// 指定する拡張子
        /// </summary>
        public string Extension
        {
            get { return this.m_fileRowCounter.Extension; }
            set
            {
                if (this.m_fileRowCounter.Extension == value)
                {
                    return;
                }

                this.m_fileRowCounter.Extension = value;
                this.RaisePropertyChanged(nameof(this.Extension));
            }
        }

        /// <summary>
        /// 無視するフォルダ
        /// </summary>
        public string ExceptDirectories
        {
            get
            {
                return string.Join(",", this.m_fileRowCounter.ExceptDirectory);
            }
            set
            {
                this.m_fileRowCounter.ExceptDirectory = value.Split(',');
                this.RaisePropertyChanged(nameof(this.ExceptDirectories));
            }
        }

        /// <summary>
        /// 数の多い順にSortラジオボタンの値
        /// </summary>
        public bool IsSort
        {
            get { return this.m_fileRowCounter.IsSort; }
            set
            {
                if (this.m_fileRowCounter.IsSort == value)
                {
                    return;
                }

                this.m_fileRowCounter.IsSort = value;
                this.RaisePropertyChanged(nameof(this.IsSort));
            }
        }

        /// <summary>
        /// 表示する最大個数
        /// </summary>
        public int MaxCount
        {
            get { return this.m_fileRowCounter.MaxCount; }
            set
            {
                if (this.m_fileRowCounter.MaxCount == value)
                {
                    return;
                }

                this.m_fileRowCounter.MaxCount = value;
                this.RaisePropertyChanged(nameof(this.MaxCount));
            }
        }

        private IEnumerable<FileList> m_fileList;

        /// <summary>
        /// ListBoxに表示する内容
        /// </summary>
        public IEnumerable<FileList> FileListIns
        {
            get { return this.m_fileList; }
            set
            {

                this.m_fileList = value;
                this.RaisePropertyChanged(nameof(this.FileListIns));
            }
        }

        #region フォルダ検索コマンド

        /// <summary>
        /// 対象のRootフォルダを選択するダイアログを立ち上げる
        /// </summary>
        public ICommand ShowSearchDirectotyCommand { get; }

        private void ShowSerchDirectory()
        {
            var dlg = new CommonOpenFileDialog("フォルダ選択");
            dlg.IsFolderPicker = true;

            //ダイアログを表示する
            var ret = dlg.ShowDialog();

            if (ret != CommonFileDialogResult.Ok)
            {
                return;
            }

            this.FolderPath = dlg.FileName;
        }

        #endregion

        #region 実行コマンド

        /// <summary>
        /// 対象のRootフォルダを選択するダイアログを立ち上げる
        /// </summary>
        public DelegateCommand ExecuteCommand { get; }

        private bool m_canExecute = true;

        private bool CanExcecute()
        {
            return this.m_canExecute;
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(this.FolderPath))
            {
                MessageBox.Show("フォルダを指定してください。");
                return false;
            }

            if (this.IsSort && this.MaxCount <= 0)
            {
                MessageBox.Show("Sortする場合、上位の表示個数は1以上に設定してください。");
                return false;
            }

            return true;
        }

        private async Task Execute()
        {
            if (!this.Validate())
            {
                return;
            }

            IDictionary<string, int> dic;

            try
            {
                this.m_canExecute = false;
                this.ExecuteCommand.RaiseCanExecuteChanged();
                dic = await this.m_fileRowCounter.CalcRowCount();
            }
            catch (Exception e)
            {
                MessageBox.Show($"ファイルの行数をカウントできませんでした。\n{e.StackTrace}");
                return;
            }
            finally
            {
                this.m_canExecute = true;
                this.ExecuteCommand.RaiseCanExecuteChanged();
            }

            var list = new List<FileList>();
            foreach (var item in dic)
            {
                list.Add(new FileList { FilePath = item.Key, RowCount = item.Value });
            }

            this.FileListIns = list;
        }

        #endregion

        /// <summary>
        /// 画面のListBoxに表示する用のItem
        /// </summary>
        public class FileList
        {
            public string FilePath { get; set; }

            public int RowCount { get; set; }
        }
    }
}