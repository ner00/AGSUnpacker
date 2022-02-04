﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using AGSUnpacker.UI.Core;
using AGSUnpacker.UI.Core.Commands;
using AGSUnpacker.UI.Models.Room;
using AGSUnpacker.UI.Service;

using Microsoft.Win32;

namespace AGSUnpacker.UI.Views.Windows
{
  internal class RoomManagerWindowViewModel : ViewModel
  {
    private readonly WindowService _windowService;

    #region Properties
    private string _title;
    public string Title
    {
      get => _title;
      set => SetProperty(ref _title, value);
    }

    private AppStatus _status;
    public AppStatus Status
    {
      get => _status;
      private set
      {
        SetProperty(ref _status, value);
        OnPropertyChanged(nameof(StatusText));
      }
    }

    // TODO(adm244): just use converters
    public string StatusText => Status.AsString();

    private int _tasksRunning;
    public int TasksRunning
    {
      get => _tasksRunning;
      set => SetProperty(ref _tasksRunning, value);
    }

    private Room _room;
    public Room Room
    {
      get => _room;
      set
      {
        SetProperty(ref _room, value);
        SaveRoomCommand?.NotifyCanExecuteChanged();
        CloseRoomCommand?.NotifyCanExecuteChanged();
      }
    }

    private RoomFrame _selectedFrame;
    public RoomFrame SelectedFrame
    {
      get => _selectedFrame;
      set
      {
        SetProperty(ref _selectedFrame, value);
        SaveImageCommand.NotifyCanExecuteChanged();
        ReplaceImageCommand.NotifyCanExecuteChanged();
      }
    }

    private int _selectedIndex;
    public int SelectedIndex
    {
      get => _selectedIndex;
      set => SetProperty(ref _selectedIndex, value);
    }
    #endregion

    #region Commands
    #region LoadRoomCommand
    private ICommand _loadRoomCommand;
    public ICommand LoadRoomCommand
    {
      get => _loadRoomCommand;
      set => SetProperty(ref _loadRoomCommand, value);
    }

    private async void OnLoadRoomCommandExecuted(object parameter)
    {
      OpenFileDialog openDialog = new OpenFileDialog()
      {
        Title = "Select room file",
        Filter = "AGS room file|*.crm",
        Multiselect = false,
        CheckFileExists = true,
        CheckPathExists = true
      };

      if (openDialog.ShowDialog(_windowService.GetWindow(this)) != true)
        return;

      Status = AppStatus.Loading;

      Room = await ModelService.LoadRoomAsync(openDialog.FileName);

      Title = openDialog.SafeFileName;
      SelectedIndex = 0;

      Status = AppStatus.Ready;
    }
    #endregion

    #region SaveRoomCommand
    private BaseCommand _saveRoomCommand;
    public BaseCommand SaveRoomCommand
    {
      get => _saveRoomCommand;
      set => SetProperty(ref _saveRoomCommand, value);
    }

    private async void OnSaveRoomCommandExecuted(object parameter)
    {
      SaveFileDialog saveDialog = new SaveFileDialog()
      {
        Title = "Save room file",
        Filter = "AGS room file|*.crm",
        CreatePrompt = false,
        OverwritePrompt = true,
      };

      if (saveDialog.ShowDialog(_windowService.GetWindow(this)) != true)
        return;

      Status = AppStatus.Busy;

      await ModelService.SaveRoomAsync(saveDialog.FileName, Room);

      Status = AppStatus.Ready;
    }

    private bool OnCanSaveRoomCommandExecuted(object parameter)
    {
      return Room != null;
    }
    #endregion

    #region CloseRoomCommand
    private BaseCommand _closeRoomCommand;
    public BaseCommand CloseRoomCommand
    {
      get => _closeRoomCommand;
      set => SetProperty(ref _closeRoomCommand, value);
    }

    private void OnCloseRoomCommandExecuted(object parameter)
    {
      Room = null;
    }

    private bool OnCanCloseRoomCommandExecuted(object parameter)
    {
      return Room != null;
    }
    #endregion

    #region QuitCommand
    private ICommand _quitCommand;
    public ICommand QuitCommand
    {
      get => _quitCommand;
      set => SetProperty(ref _quitCommand, value);
    }

    private void OnQuitCommandExecuted(object parameter)
    {
      _windowService.Close(this);
    }
    #endregion

    #region SaveImageCommand
    private AsyncBaseCommand _saveImageCommand;
    public AsyncBaseCommand SaveImageCommand
    {
      get => _saveImageCommand;
      set => SetProperty(ref _saveImageCommand, value);
    }

    private Task OnSaveImageCommandExecuted(object parameter)
    {
      SaveFileDialog saveDialog = new SaveFileDialog()
      {
        Title = "Save image",
        Filter = "PNG file|*.png",
        CreatePrompt = false,
        OverwritePrompt = true,
      };

      if (saveDialog.ShowDialog(_windowService.GetWindow(this)) != true)
        return Task.CompletedTask;

      BitmapSource image = (BitmapSource)SelectedFrame.Source.GetAsFrozen();

      return Task.Run(
        () =>
        {
          using (FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.Write))
          {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Interlace = PngInterlaceOption.Off;
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
          }
        }
      );
    }

    private bool OnCanSaveImageCommandExecuted(object parameter)
    {
      return !SaveImageCommand.IsExecuting && SelectedFrame != null;
    }
    #endregion

    #region ReplaceImageCommand
    private BaseCommand _replaceImageCommand;
    public BaseCommand ReplaceImageCommand
    {
      get => _replaceImageCommand;
      set => SetProperty(ref _replaceImageCommand, value);
    }

    private async void OnReplaceImageCommandExecuted(object parameter)
    {
      OpenFileDialog openDialog = new OpenFileDialog()
      {
        Title = "Select image file",
        Filter = "PNG image|*.png",
        Multiselect = false,
        CheckFileExists = true,
        CheckPathExists = true
      };

      if (openDialog.ShowDialog(_windowService.GetWindow(this)) != true)
        return;

      Status = AppStatus.Loading;

      Graphics.Bitmap image = await Task.Run(
        () => new Graphics.Bitmap(openDialog.FileName)
      );

      Room.ChangeFrame(SelectedIndex, image);

      Status = AppStatus.Ready;
    }

    private bool OnCanReplaceImageCommandExecuted(object parameter)
    {
      return (Status == AppStatus.Ready) && SelectedFrame != null;
    }
    #endregion
    #endregion

    private void OnIsExecutingChanged(object sender, bool newValue)
    {
      TasksRunning += newValue ? 1 : -1;

      if (TasksRunning < 0)
        TasksRunning = 0;

      Status = TasksRunning > 0 ? AppStatus.Busy : AppStatus.Ready;
    }

    public RoomManagerWindowViewModel(WindowService windowService)
    {
      _windowService = windowService;

      Room = null;
      Status = AppStatus.Ready;

      LoadRoomCommand = new ExecuteCommand(OnLoadRoomCommandExecuted);
      SaveRoomCommand = new ExecuteCommand(OnSaveRoomCommandExecuted, OnCanSaveRoomCommandExecuted);
      CloseRoomCommand = new ExecuteCommand(OnCloseRoomCommandExecuted, OnCanCloseRoomCommandExecuted);
      QuitCommand = new ExecuteCommand(OnQuitCommandExecuted);

      SaveImageCommand = new AsyncExecuteCommand(OnSaveImageCommandExecuted, OnCanSaveImageCommandExecuted);
      SaveImageCommand.IsExecutingChanged += OnIsExecutingChanged;

      ReplaceImageCommand = new ExecuteCommand(OnReplaceImageCommandExecuted, OnCanReplaceImageCommandExecuted);
    }
  }
}
