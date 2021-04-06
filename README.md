# SrvWrap
A simple utility to install any executable as a Windows Service.
## The Configuration File
The configuration file is a core of separate installation. It is possible to install as many as needed services using a separate configuration file for each. Each configuration file should be at a separate directory and have fixed name `srvwrap.config.exe`. The executable srvwrap.exe should not be copied.
````
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <service>
    <name>EDI_test</name>
    <display-name>EDI test service</display-name>
    <description>EDI test service for srvwrap testing</description>
    <starttype>Automatic</starttype>
    <account>LocalSystem</account>
  </service>
  <runtime>
    <env name="JAVA_HOME" value="C:\Program Files\Java\jdk1.8.0_221"/>
    <executable>%JAVA_HOME%\bin\java.exe</executable>
    <env name="HELLOJAVA_JAR" value="F:\leksi\C#\srvwrap\hellojava\dist\hellojava.jar"/>
    <args>-jar %HELLOJAVA_JAR%</args>
    <stdout.log>logs\stdout.log</stdout.log>
    <error.log>logs\error.log</error.log>
  </runtime>
  <templates>
    <service>
      <name></name>
      <display-name></display-name>
      <description></description>
      <account>User (default) | LocalService | LocalSystem | NetworkService</account>
      <username></username>
      <password></password>
      <starttype>Automatic | Manual | Disabled</starttype>
    </service>
    <runtime>
      <env name="" value=""/>
      <env name="" value=""/>
      <env name="" value=""/>
      <executable></executable>
      <args></args>
      <stdout.log></stdout.log>
      <error.log></error.log>
    </runtime>
  </templates>
</configuration>
````
A configuration file consists of three elements: `<service/>`, `<runtime/>` and `<templates/>`. The `<service/>` element  describes options that represent the application at the Services. The `<name/>` element should be unique for the host computer. The `<runtime/>` element configurates the usage of the application to be a service. It may contain several `<env/>` elements that describe environment variables. These environment variables are available from inside the application and may be used at any elements under `<runtime/>`. `<executable/>` element describes a path to executable file being a service. `<args/>` describes optional command line arguments passed to the executable. `<stdout.log/>' and `<error.log/>` describes log files for stdout and err respectively. All paths may be absolute or relational to configuration file directory.
## The Service Installation
1. Change current directory to one containing the configuration file.
2. Run .NET's InstallUtil with srvwrap.exe as an argument.

For example:
Suppose we have the configuration file `srvwrap.config.exe` at F:\leksi\tmp and `srvwrap.exe` at F:\leksi\C#\srvwrap\bin\Debug.
````
C:>cd /d F:\leksi\tmp
F:\leksi\tmp> C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe F:\leksi\C#\srvwrap\bin\Debug\srvwrap.exe 
````
