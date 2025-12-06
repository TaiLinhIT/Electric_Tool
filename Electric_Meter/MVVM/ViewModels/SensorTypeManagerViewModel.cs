using System.Collections.ObjectModel;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Electric_Meter.Dto.SensorTypeDto;
using Electric_Meter.Interfaces;
using Electric_Meter.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Electric_Meter.MVVM.ViewModels
{

    public partial class SensorTypeManagerViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly LanguageService _languageService;
        private readonly IService _service;
        private readonly IServiceScopeFactory _scopeFactory;
        #endregion
        public SensorTypeManagerViewModel(LanguageService languageService, IService service, IServiceScopeFactory serviceScopeFactory)
        {
            _languageService = languageService;
            _languageService.LanguageChanged += UpdateTexts;
            _service = service;
            _scopeFactory = serviceScopeFactory;
            UpdateTexts();
            LoadSensorTypeAsync();
            IsEnabledBtnAdd = true;
            IsEnabledBtnEdit = false;
            IsEnableBtnDelete = false;
        }
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            TypeIdText = _languageService.GetString("Type id");
            NameText = _languageService.GetString("Name");
            AddCommandText = _languageService.GetString("Add");
            UpdateCommandText = _languageService.GetString("Edit");
            DeleteCommandText = _languageService.GetString("Delete");

        }
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string typeIdText;
        [ObservableProperty] private string nameText;

        #endregion
        #region [ Properties ]
        [ObservableProperty] private int typeId;
        [ObservableProperty] private string name;
        [ObservableProperty] private ObservableCollection<SensorTypeDto> lstSensorType;
        [ObservableProperty] private string addCommandText;
        [ObservableProperty] private string updateCommandText;
        [ObservableProperty] private string deleteCommandText;
        [ObservableProperty] private SensorTypeDto selectedSensorType;
        [ObservableProperty] private bool isEnabledBtnAdd;
        [ObservableProperty] private bool isEnabledBtnEdit;
        [ObservableProperty] private bool isEnableBtnDelete;
        partial void OnSelectedSensorTypeChanged(SensorTypeDto value)
        {
            if (value == null)
            {
                TypeId = 0;
                Name = string.Empty;
                return;
            }
            Name = value.Name;
            TypeId = value.TypeId;

            IsEnabledBtnEdit = true;
            AddSensorTypeCommand.NotifyCanExecuteChanged();
            EditSensorTypeCommand.NotifyCanExecuteChanged();
            DeleteSensorTypeCommand.NotifyCanExecuteChanged();

        }
        #endregion
        #region [ Command Logic ]
        [RelayCommand(CanExecute = nameof(CanExecuteAddSensorType))]
        private async Task AddSensorType()
        {
            try
            {
                var dto = new SensorTypeDto
                {
                    Name = Name,
                    TypeId = TypeId,
                };
                await _service.AddSensorTypeAsync(dto);
                await LoadSensorTypeAsync();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private async Task LoadSensorTypeAsync()
        {
            try
            {
                var lst = await _service.GetSensorTypesAsync();
                LstSensorType = new(lst);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private bool CanExecuteAddSensorType() => true;
        [RelayCommand(CanExecute = nameof(CanExecuteEditSensorType))]
        private async Task EditSensorType()
        {
            try
            {
                if (TypeId == 0)
                {
                    MessageBox.Show("Not find"); return;
                }

                var dto = new SensorTypeDto
                {
                    TypeId = TypeId,
                    Name = Name,
                };
                await _service.UpdateSensorTypeAsync(dto);
                await LoadSensorTypeAsync();
                MessageBox.Show("Successfully!");
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private bool CanExecuteEditSensorType() => TypeId != 0;
        [RelayCommand(CanExecute = nameof(CanExecuteDeleteSensorType))]
        private async Task DeleteSensorType()
        {
            try
            {
                if (TypeId == 0)
                {
                    MessageBox.Show("Not find"); return;
                }


                await _service.DeleteSensorTypeAsync(TypeId);
                await LoadSensorTypeAsync();
                MessageBox.Show("Successfully!");
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private bool CanExecuteDeleteSensorType() => TypeId != 0;
        #endregion
    }
}
