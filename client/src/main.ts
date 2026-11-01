import "./style.css";
import { HubConnectionBuilder } from "@microsoft/signalr";

const joinForm = document.getElementById("join-form") as HTMLFormElement;
const displayNameInput = document.getElementById("display-name") as HTMLInputElement;
const usersList = document.getElementById("users") as HTMLUListElement;

const connection = new HubConnectionBuilder()
  .withUrl("/chat")
  .build();

connection.on("PresenceUpdated", (users: string[]) => {
  usersList.innerHTML = "";
  for (const name of users) {
    const li = document.createElement("li");
    li.textContent = name;
    usersList.appendChild(li);
  }
});

async function start(): Promise<void> {
  try {
    await connection.start();
  } catch (error) {
    console.error("SignalR connection failed:", error);
  }
}

start();

joinForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const name = displayNameInput.value.trim();
  if (!name) return;
  try {
    await connection.invoke("Join", name);
    displayNameInput.value = "";
  } catch (error) {
    console.error("Join failed:", error);
  }
});