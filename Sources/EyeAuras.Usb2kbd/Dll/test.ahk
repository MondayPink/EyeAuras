MsgBox, 123
Sleep, 2000

hModule := DllCall("LoadLibrary", "AStr", "usb2kbd.dll")  ; Подгрузим Dll в начале скрипта, чтоб не подгружать постоянно

hFile := DllCall("usb2kbd\hasso", int, 1, int, 11, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3
hFile := DllCall("usb2kbd\hasso", int, 8, int, 11, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3

hFile := DllCall("usb2kbd\hasso", int, 1, int, 8, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3
hFile := DllCall("usb2kbd\hasso", int, 8, int, 8, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3

hFile := DllCall("usb2kbd\hasso", int, 1, int, 15, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3
hFile := DllCall("usb2kbd\hasso", int, 8, int, 15, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3

hFile := DllCall("usb2kbd\hasso", int, 1, int, 15, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3
hFile := DllCall("usb2kbd\hasso", int, 8, int, 15, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3

hFile := DllCall("usb2kbd\hasso", int, 1, int, 18, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3
hFile := DllCall("usb2kbd\hasso", int, 8, int, 18, int, 0, int, 0, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3

hFile := DllCall("usb2kbd\hasso", int, 3, int, 0, int, 100, int, 100, "AStr", "vid_046d&pid_c212", int, 1212)
Sleep, 3
MsgBox, 345

DllCall("FreeLibrary", "UInt", hModule)  ; Выгрузим Dll, чтоб освободить память