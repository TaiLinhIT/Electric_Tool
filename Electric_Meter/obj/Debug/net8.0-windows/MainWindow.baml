   M S B A M L   `   `   `  ����I  EElectric_Meter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullO,clr-namespace:Electric_Meter.MVVM.ViewModelsElectric_Meter.MVVM.ViewModels  5   	   E'clr-namespace:Electric_Meter.MVVM.ViewsElectric_Meter.MVVM.Views  5   	   =#clr-namespace:Electric_Meter.ModelsElectric_Meter.Models  5   	       Electric_Meter.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   ;vm,clr-namespace:Electric_Meter.MVVM.ViewModels     5   	   9views'clr-namespace:Electric_Meter.MVVM.Views     5   	   _ [Microsoft.Xaml.Behaviors, Version=1.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a3i+http://schemas.microsoft.com/xaml/behaviors  5   	   0models#clr-namespace:Electric_Meter.Models  5   	     � Title$  
MainWindow��5   	   $	��800��6+    � WindowState 5   	   �� *MainWindowState+ �� Mode$ TwoWay=� � 	Resources 5          BooleanToVisibilityConverter��%   &	        �� 5   
   6'   5      .��5      �  � ColumnDefinitions 5   
   �� 5      $
��133* �6   �� 5      $	��11* �6   �� 5      $	��56* �6    � RowDefinitions 5   
   �� 5      $
��auto �6   �� 5      $
��auto �6   �� 5      $�� auto �6   .+�5    
   ��  �� CornerRadius$ 20��6   $��0��6#   $��3��60   ._�5!      � $��0��6   
������$��3��68    5"      �� 5#      $��2* �6'   �� 5$      $��* �6'   �� 5%      $��* �6'   �� 5&      $��2* �6'   �� 5'      $��* �6'   �� 5(      $��* �6'   .+�5*      �� $��0��6   $��Center=�5+      $��Center=�6*   $��DemiBold"�6E   ��5*   ,   ��  �� Path$ DeviceConfig.NameStyle� �� StringFormat$ Name style: {0}��5+   E   �� 5,      $��1��6   $��Center=�5-      $��Center=�6*   $��DemiBold"�6E   ��5,   ,   �� $ DeviceConfig.Max�$ Max: {0}��5-   E   �� 5.      $��2��6   $��Center=�5/      $��Center=�6*   $��DemiBold"�6E   ��5.   ,   �� $ DeviceConfig.Min�$ Min: {0}��5/   E   �� 50      $��3��6   $��Center=�51      $��Center=�6*   $��DemiBold"�6E   ��50   ,   �� $ DeviceConfig.Baudrate�$ Baudrate: {0}��51   E   �� 52      $��4��6   $��Center=�53      $��Center=�6*   $��DemiBold"�6E   ��52   ,   �� $ DeviceConfig.Port�$ 	Port: {0}��53   E   �� 54      $��5��6   $��0 0 10 0q�6,   $��Center=�55      $��Center=�6*   $��DemiBold"�6E   ��54   >   �� *CurrentFactory+$ Factory: {0}��55   E   56      57      �� 59   
   $��1��6   $��0 1 0 0q�6   
�������$��3��6K   $��0,0,0,3q�6_   ._�5:      � 
������� 5;      �� 5<      $��* �6'   �� 5=      $��* �6'   �� 5>      $��* �6'   �� 5?      $��* �6'   .+�5A      �� $��0��6   $��5 0 5 0q�6)   ��6:   �� *SettingCommandText+��6a   �� *SettingCommand+�� 5B      $��1��6   ��6)   �� *HelpCommandText+��6M   �� *DataFormCommand+�� 5C      $��2��6   $��Center=�6,   $��Right=�6G   $��0 0 27 0q�6c   Select Language:6�   �� 5D      	 �� Text$	 Select Language:��6   
 �� 
IsEditable$
 False��5F       �� SelectedValuePath$	 Tag��5H      $��3��5I       �� SelectedValue 5E      �� *SelectedLanguage+$ TwoWay=�.@�5K      �� $��English��6#   
 � Tag$ en��65   �� 5L      $��Tiếng Việt��6#   $ vi��68   �� 5M      $��中文��6#   $ cn��60   �� 5N      $��ខ្មែរ��6#   $ km��63   5O      5P      5Q      �� 5R   
   $��2��6   $��0 1 0 0q�6   
�������$��3��6K   ._�5S      �  5T      �� 5U      $	��170 �6'   �� 5V      $��* �6'   .+�5Y      � $��0��6   .+�5Z      �� $��0 0 1 0q�6   
�������._�5[      � $��Stretch=�6   $��0,0,-1,0q�6;    5\      �� 5]   "   $
��Auto �60   �� 5^   "   $	��500 �60   .+�5b      �� $��0��6(   $
��MENU��65   $��Center=�6A   $��Center=�6\   $��18(�5c      $��10 10q�6&   ���#a3a3a3$
��Bold"�6J   �� 5f      $��1��6+   $
��Auto=�68   .��5g   "   �� ��6/   �� *
Machines+��5h   &   �� 5i   *   .R�5j   .   �� $��0 0 0 10q�69   .+�5l   2   �� $��40��69   $��15(�6E   $
��10,0q�6l   ��6S   �� *Name+��5m   6   �� 5n   :    �� 
TargetType" ��6@   .�5o   >   ��  �� Property � �� �� Value
 ����6[   �� 5p   >    � ��
 �����6[   �� 5q   >    � ��$
 Bold"�6[   �� 5r   >    � ��
 ���� 6\   �� 5s   >    �@�Cursor$
 Hand��6W    �� Triggers 5t   >   �� 5u   B    �� Binding 6N   �� *
LineCode+ �� Value H6k   .a�5v   F   ��  � ��
 �  ��6c   5w   C   �� 5x   B    6N   �� *
LineCode+ C6k   .a�5y   F   ��  � ��
 ��ح�6c   5z   C   P� 5{   B    P� Property �@J�IsMouseOver P� Value$
 True��6a   . �5|   F   ��  � �� �	#FF88C0D06c   5}   C   P� 5~   B    � ��$
 True��6_   . �5   F   ��  � �� �	#FF5E81AC6c   5�   C   5�   ;    J� InputBindings 5�   6   d� 5�   :    d� Gesture$ 
RightClicka�6G    �� Command 5�      �� * DataContext.OpenSettingCommand+ �� RelativeSource ��  �� AncestorType
 Window �� CommandParameter 5�      �� 5�   3   �� 5�   2   $��35��69   $��15(�6E   
����� �$��20,0,0,0q�5�   6   ��5�      �� *DataContext.OpenToolCommand+ �� 
 Window��5�      �� .��5�   6   ��  �� Orientation$ 
Horizontal=�6A   .+�5�   :   �� $��Thành Hình ��6D   �� 5�   :   ��6D   �� *Line+5�   7   5�   3   5�   /   5�   +   5�   #   5�      5�      5�      5�      � 5�      $��1��6   $��	10,10,0,0q�6'    5�      �� 5�      $
��auto �6(   �� 5�      $
��auto �6(   �� 5�      $
��auto �6(   .+�5�      �� $��0��6%   $��	0,0,0,243q�5�   %   ��5�   2   �� *CurrentViewModel+ 5�       �� DataType5   /Electric_Meter.MVVM.ViewModels.SettingViewModel+   %Electric_Meter.MVVM.Views.SettingView2   ,Electric_Meter.MVVM.ViewModels.ToolViewModel(   "Electric_Meter.MVVM.Views.ToolView��%�   (��       5�   +   *, +)(�� 2     5�   +   *, +)�� 5�      "  6+   .R�5�   "     5�      �� 5�      "  6+   .R�5�   "     5�      5�      5�      5�      5�      5�      5�      5�      