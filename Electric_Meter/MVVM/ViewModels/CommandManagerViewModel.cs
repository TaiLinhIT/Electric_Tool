using System.Collections.ObjectModel;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Electric_Meter.Dto.CodeTypeDto;
using Electric_Meter.Interfaces;
using Electric_Meter.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Electric_Meter.MVVM.ViewModels
{
    public partial class CommandManagerViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly LanguageService _languageService;
        private readonly IService _service;
        private readonly IServiceScopeFactory _scopeFactory;
        #endregion
        public CommandManagerViewModel(LanguageService languageService, IService service, IServiceScopeFactory serviceScopeFactory)
        {
            _languageService = languageService;
            _languageService.LanguageChanged += UpdateTexts;
            _service = service;
            _scopeFactory = serviceScopeFactory;
            UpdateTexts();
            LoadCodeTypeAsync();
            IsEnabledBtnAdd = true;
            IsEnabledBtnEdit = false;
            IsEnableBtnDelete = false;
        }
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            CodeTypeText = _languageService.GetString("Code type");
            NameText = _languageService.GetString("Name");
            AddCommandText = _languageService.GetString("Add");
            UpdateCommandText = _languageService.GetString("Edit");
            DeleteCommandText = _languageService.GetString("Delete");

        }
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string codeTypeText;
        [ObservableProperty] private string nameText;

        #endregion
        #region [ Properties ]
        [ObservableProperty] private int id;
        [ObservableProperty] private string name;
        [ObservableProperty] private string commandType;
        [ObservableProperty] private ObservableCollection<CodeTypeDto> lstCodeType;
        [ObservableProperty] private string addCommandText;
        [ObservableProperty] private string updateCommandText;
        [ObservableProperty] private string deleteCommandText;
        [ObservableProperty] private CodeTypeDto selectedCodeType;
        [ObservableProperty] private bool isEnabledBtnAdd;
        [ObservableProperty] private bool isEnabledBtnEdit;
        [ObservableProperty] private bool isEnableBtnDelete;
        partial void OnSelectedCodeTypeChanged(CodeTypeDto value)
        {
            if (value == null)
            {
                Id = 0;
                Name = string.Empty;
                CommandType = string.Empty;
                return;
            }
            Name = value.NameCodeType;
            Id = value.Id;
            CommandType = value.CodeTypeId;
            IsEnabledBtnEdit = true;
            AddTypeCommand.NotifyCanExecuteChanged();
            EditTypeCommand.NotifyCanExecuteChanged();
            DeleteTypeCommand.NotifyCanExecuteChanged();

        }
        #endregion
        #region [ Command Logic ]
        [RelayCommand(CanExecute = nameof(CanExecuteAddType))]
        private async Task AddType()
        {
            try
            {
                var dto = new CodeTypeDto
                {
                    NameCodeType = Name,
                    CodeTypeId = CommandType
                };
                await _service.AddCodeTypeAsync(dto);
                await LoadCodeTypeAsync();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private async Task LoadCodeTypeAsync()
        {
            try
            {
                var lst = await _service.GetCodeTypeAsync();
                LstCodeType = new(lst);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private bool CanExecuteAddType() => true;
        [RelayCommand(CanExecute = nameof(CanExecuteEditType))]
        private async Task EditType()
        {
            try
            {
                if (Id == 0)
                {
                    MessageBox.Show("Not find"); return;
                }

                var dto = new CodeTypeDto
                {
                    Id = Id,
                    NameCodeType = Name,
                    CodeTypeId = CommandType
                };
                await _service.UpdateCodeTypeAsync(dto);
                await LoadCodeTypeAsync();
                MessageBox.Show("Successfully!");
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private bool CanExecuteEditType() => CommandType != null;
        [RelayCommand(CanExecute = nameof(CanExecuteDeleteType))]
        private async Task DeleteType()
        {
            try
            {
                if (Id == 0)
                {
                    MessageBox.Show("Not find"); return;
                }


                await _service.DeleteCodeTypeAsync(Id);
                await LoadCodeTypeAsync();
                MessageBox.Show("Successfully!");
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private bool CanExecuteDeleteType() => CommandType != null;
        #endregion
    }
}
