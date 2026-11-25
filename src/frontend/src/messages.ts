export type FailedRowReason = 'InvalidDate' | 'InvalidAmount' | 'InvalidBalance'

export type Bank = 'Ruralvia' | 'Revolut'

export type Category =
  | 'Interest'
  | 'Salary'
  | 'Savings'
  | 'SavingsWithdrawal'
  | 'Internal'
  | 'Groceries'
  | 'Restaurants'
  | 'Clothing'
  | 'Footwear'
  | 'Jewelry'
  | 'Home'
  | 'Health'
  | 'Transport'
  | 'Travel'
  | 'Pets'
  | 'Cash'
  | 'TransfersOut'
  | 'TransfersIn'
  | 'Uncategorized'

export type RuleSign = 'Any' | 'Expense' | 'Income'

export interface Rule {
  id: number
  pattern: string
  category: Category
  sign: RuleSign
  priority: number
}

export type StatementError = 'UnsupportedStatement' | 'ProcessingFailed'

export type SignUpError = 'EmailAlreadyRegistered' | 'InvalidEmail' | 'InvalidPassword' | 'SignUpFailed'

export type CorrectionError = 'CorrectionFailed'

export type RuleError = 'InvalidRulePattern' | 'RuleAlreadyExists' | 'RuleCreationFailed' | 'RuleUpdateFailed'

export type LogInError = 'InvalidCredentials' | 'LogInFailed'

export const logInErrorMessages: Record<LogInError, string> = {
  InvalidCredentials: 'El correo electrónico o la contraseña no son válidos',
  LogInFailed: 'No se pudo iniciar sesión',
}

export const bankNames: Record<Bank, string> = {
  Ruralvia: 'Ruralvía',
  Revolut: 'Revolut',
}

export const categoryNames: Record<Category, string> = {
  Interest: 'Intereses',
  Salary: 'Nómina',
  Savings: 'Ahorro',
  SavingsWithdrawal: 'Salida del ahorro',
  Internal: 'Movimientos internos',
  Groceries: 'Supermercado',
  Restaurants: 'Restaurantes y cafeterías',
  Clothing: 'Ropa',
  Footwear: 'Calzado',
  Jewelry: 'Joyería',
  Home: 'Hogar',
  Health: 'Salud',
  Transport: 'Transporte y vehículo',
  Travel: 'Viajes',
  Pets: 'Mascotas',
  Cash: 'Efectivo',
  TransfersOut: 'Transferencias y Bizum enviados',
  TransfersIn: 'Transferencias y Bizum recibidos',
  Uncategorized: 'Sin categoría',
}

export const ruleSignNames: Record<RuleSign, string> = {
  Any: 'Movimiento',
  Expense: 'Gasto',
  Income: 'Ingreso',
}

// A "*" at the start or the end of a pattern relaxes that side of the whole-word match.
export function describeRule({ pattern, sign }: Pick<Rule, 'pattern' | 'sign'>): string {
  const word = pattern.replace(/^\*|\*$/g, '')
  const startsOpen = pattern.startsWith('*')
  const endsOpen = pattern.endsWith('*')
  const match =
    startsOpen && endsOpen
      ? `cuya descripción contiene «${word}»`
      : endsOpen
        ? `con una palabra que empieza por «${word}»`
        : startsOpen
          ? `con una palabra que termina en «${word}»`
          : `con la palabra «${word}»`
  return `${ruleSignNames[sign]} ${match}`
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
  UnsupportedStatement: 'El formato del fichero no es compatible',
  ProcessingFailed: 'No se pudo procesar el fichero',
}

export type ApplyRulesError = 'ApplyRulesFailed'

export const applyRulesErrorMessages: Record<ApplyRulesError, string> = {
  ApplyRulesFailed: 'No se pudieron aplicar las reglas',
}

export function describeAppliedRules(changed: number): string {
  const result = changed === 1 ? 'movimiento cambió' : 'movimientos cambiaron'
  return `Reglas aplicadas: ${changed} ${result} de categoría`
}

export type RulesLoadError = 'RulesLoadFailed'

export const rulesLoadErrorMessages: Record<RulesLoadError, string> = {
  RulesLoadFailed: 'No se pudieron cargar las reglas',
}

export const correctionErrorMessages: Record<CorrectionError, string> = {
  CorrectionFailed: 'No se pudo guardar la categoría',
}

export const ruleErrorMessages: Record<RuleError, string> = {
  InvalidRulePattern: 'El patrón no es válido: necesita una palabra y como mucho un «*» en cada extremo',
  RuleAlreadyExists: 'Ya existe una regla con ese patrón y ese signo',
  RuleCreationFailed: 'No se pudo crear la regla',
  RuleUpdateFailed: 'No se pudo cambiar la categoría de la regla',
}
