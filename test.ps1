$asm = [Reflection.Assembly]::LoadFrom('C:\Users\johnt\.nuget\packages\dynamovisualprogramming.wpfuilibrary\4.1.1.5050\lib\net10.0\DynamoCoreWpf.dll')
$asm.GetTypes() | Where-Object { $_.Name -eq 'NoteViewModel' } | ForEach-Object { $_.GetMethods([System.Reflection.BindingFlags]'Instance, Public, NonPublic') } | Where-Object { $_.Name -match 'pin|Pin' } | Select-Object Name
