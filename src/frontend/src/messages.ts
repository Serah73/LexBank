export type FailedRowReason = 'InvalidDate' | 'InvalidAmount' | 'InvalidBalance'

export type StatementError = 'NotRuralviaStatement'

export const failedRowMessages: Record<FailedRowReason, string> = {
  InvalidDate: 'Fecha no válida',
  InvalidAmount: 'Importe no válido',
  InvalidBalance: 'Saldo no válido',
}

export const statementErrorMessages: Record<StatementError, string> = {
  NotRuralviaStatement: 'El fichero no es un extracto XLSX de Ruralvía',
}
