import Logo from '@/components/Logo';
import SearchBar from '@/components/SearchBar';
import AccountButton from '@/components/AccountButton';
import ModeToggle from './ModeToggle';

const AdminHeader = () => {
  return (
    <header className="bg-background w-full border-b">
  <div className="align-element flex items-center justify-between gap-2 py-1.5">
        <div className="flex items-center gap-2 min-w-[100px]">
          <Logo />
        </div>
        <div className="flex-1 flex justify-center">
          <SearchBar />
        </div>
        <div className="flex items-center gap-3 min-w-[160px] justify-end">
          <AccountButton />
          <ModeToggle />
        </div>
      </div>
    </header>
  );
};

export default AdminHeader;
