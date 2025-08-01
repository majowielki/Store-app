import axios from "axios";

//const productionUrl = "https://strapi-store-server.onrender.com/api";
const productionUrl = "https://localhost:44329/api";

export const customFetch = axios.create({
  baseURL: productionUrl,
});
