import { createRoot } from "react-dom/client";
import App from "./App.tsx";
import "./index.css";
import { Provider } from "react-redux";
import { store } from "./store";
import { Toaster } from "@/components/ui/toaster";

createRoot(document.getElementById("root")!).render(
  <Provider store={store}>
    <Toaster />
    <App />
  </Provider>
);
