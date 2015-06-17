@Echo Off

C:\Windows\System32\inetsrv\appcmd.exe add app /site.name:"Default Web Site" /path:/EQArchitect /physicalPath:C:\inetpub\wwwroot\EQArchitect

If ErrorLevel 1 GoTo :Error

Echo.
Echo   Completed. Press any key to close...
Pause > nul
GoTo :EOF

:Error
Echo.
Echo   Note: You must run this batch file with admin rights to add the
Echo         EQArchitect application to your web server's compilation.
Echo.
Echo   Press any key to close...
Pause > nul
GoTo :EOF
