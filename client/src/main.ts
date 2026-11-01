import "./style.css";
import { HubConnectionBuilder } from "@microsoft/signalr";

const joinForm = document.getElementById("join-form") as HTMLFormElement;
const displayNameInput = document.getElementById("display-name") as HTMLInputElement;
const usersList = document.getElementById("users") as HTMLUListElement;

const messageForm = document.getElementById("message-form") as HTMLFormElement;
const messageInput = document.getElementById("message-input") as HTMLInputElement;
const messagesList = document.getElementById("messages") as HTMLUListElement;

const typingIndicator = document.getElementById("typing-indicator") as HTMLElement;


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

connection.on("MessageReceived", (name: string, text: string) => {
  const li = document.createElement("li");
  li.textContent = `${name}: ${text}`;
  messagesList.appendChild(li);
});

let typingTimeout: ReturnType<typeof setTimeout> | null = null;
connection.on("UserTyping", (name: string) => {
  typingIndicator.textContent = `${name} is typing...`;
  if (typingTimeout) clearTimeout(typingTimeout);
  typingTimeout = setTimeout(() => {
    typingIndicator.textContent = "";
  }, 2000);
});

connection.on("MessageReceived", (name: string, text: string) => {
  typingIndicator.textContent = "";
  if (typingTimeout) clearTimeout(typingTimeout);
  const li = document.createElement("li");
  li.textContent = `${name}: ${text}`;
  messagesList.appendChild(li);
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

messageForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const text = messageInput.value.trim();
  if (!text) return;
  try {
    await connection.invoke("SendMessage", text);
    messageInput.value = "";
  } catch (error) {
    console.error("Send failed:", error);
  }
});

let lastTypingSent = 0;
messageInput.addEventListener("input", () => {
  const now = Date.now();
  if (now - lastTypingSent < 1500) return;
  lastTypingSent = now;
  connection.invoke("UserTyping").catch(() => { });
});