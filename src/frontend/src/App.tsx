import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import Layout from './Layout'
import LogInPage from './LogInPage'
import RulesPage from './RulesPage'
import TransactionsPage from './TransactionsPage'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LogInPage />} />
        <Route element={<Layout />}>
          <Route path="/transactions" element={<TransactionsPage />} />
          <Route path="/rules" element={<RulesPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/transactions" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
