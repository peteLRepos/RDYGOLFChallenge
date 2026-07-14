import { Routes, Route } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { LoginScreen } from './components/LoginScreen';
import { Header } from './components/Header';
import { CoursesPage } from './pages/CoursesPage';
import { TeeSheetPage } from './pages/TeeSheetPage';
import './App.css';

function App() {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <LoginScreen />;
  }

  return (
    <>
      <Header title="Golf Club Admin" />
      <Routes>
        <Route path="/" element={<CoursesPage />} />
        <Route path="/courses/:resourceId" element={<TeeSheetPage />} />
      </Routes>
    </>
  );
}

export default App;
