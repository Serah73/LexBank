import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import LogInPage from './LogInPage'
import TransactionsPage from './TransactionsPage'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LogInPage />} />
        <Route path="/transactions" element={<TransactionsPage />} />
        <Route path="*" element={<Navigate to="/transactions" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
