export type FailedRowReason = 'InvalidDate' | 'InvalidAmount' | 'InvalidBalance'

export type StatementError = 'NotRuralviaStatement' | 'ProcessingFailed'

export type SignUpError = 'EmailAlreadyRegistered' | 'InvalidEmail' | 'InvalidPassword' | 'SignUpFailed'

export type LogInError = 'InvalidCredentials' | 'LogInFailed'

export const logInErrorMessages: Record<LogInError, string> = {
  InvalidCredentials: 'El correo electrónico o la contraseña no son válidos',
  LogInFailed: 'No se pudo iniciar sesión',
}

export const failedRowMessages: Record<FailedRowReason, string> = {
  InvalidDate: 'Fecha no válida',
  InvalidAmount: 'Importe no válido',
  InvalidBalance: 'Saldo no válido',
}

export const signUpErrorMessages: Record<SignUpError, string> = {
  EmailAlreadyRegistered: 'Ese correo electrónico ya está en uso',
  InvalidEmail: 'El correo electrónico no es válido',
  InvalidPassword: 'La contraseña debe tener entre 8 y 128 caracteres',
  SignUpFailed: 'No se pudo crear la cuenta',
}

export const statementErrorMessages: Record<StatementError, string> = {
  NotRuralviaStatement: 'El fichero no es un extracto XLSX de Ruralvía',
  ProcessingFailed: 'No se pudo procesar el fichero',
}
