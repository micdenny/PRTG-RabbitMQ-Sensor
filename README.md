# PRTG-RabbitMQ-Sensor
A RabbitMQ Sensor for PRTG

# Requirements
* .NET Framework 4.5
* PRTG Version 16 (Does not work in PRTG Version 14)
* RabbitMQ Server

# Usage

## Run the executable
`Prtg.RabbitMq.Sensor.exe <ServerAndPort> <User> <Password> <Type> <Host?> <Name?>`

## Add the sensor on PRTG
1. Copy the binary `Prtg.RabbitMq.Sensor.exe` to the custom sensors folder of the probe `%programfiles(x86)%\PRTG Network Monitor\Custom Sensors\EXEXML\`
1. Add an `EXE/Script Advanced` sensor to a device
2. Select `Prtg.RabbitMq.Sensor.exe` from the EXE/Script list
3. Set the parameters to pass to the executable (see the samples)

## Sensor parameters samples

### Read the overview statistics
- `myrabbit.domain.local:15672 someuser somepassword overview`

### Read a specific queue statistics
- `myrabbit.domain.local:15672 someuser somepassword queues / NameOfTheQueue`
- `myrabbit.domain.local:15672 someuser somepassword queues NameOfTheVirtualHost NameOfTheQueue`
