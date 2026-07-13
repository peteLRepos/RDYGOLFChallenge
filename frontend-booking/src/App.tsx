import { Routes, Route } from 'react-router-dom';
import { Header } from './components/Header';
import { CoursesPage } from './pages/CoursesPage';
import { TeeSheetPage } from './pages/TeeSheetPage';
import { MyBookingsPage } from './pages/MyBookingsPage';
import './App.css';

function App() {
  return (
    <>
      <Header title="Golf Club Booking" />
      <Routes>
        <Route path="/" element={<CoursesPage />} />
        <Route path="/courses/:resourceId" element={<TeeSheetPage />} />
        <Route path="/my-bookings" element={<MyBookingsPage />} />
      </Routes>
    </>
  );
}

export default App;
