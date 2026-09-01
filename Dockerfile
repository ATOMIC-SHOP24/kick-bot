# Use a lightweight Linux environment
FROM debian:latest

# Install necessary tools and Linux dependencies for MCC
RUN apt-get update && apt-get install -y wget ca-certificates libicu-dev

# Create a folder for the bot
WORKDIR /bot

# Copy your configuration and script into the container
COPY MinecraftClient.ini .
COPY autosell.cs .

# Download the Linux x64 version of Minecraft Console Client
RUN wget https://github.com/MCCTeam/Minecraft-Console-Client/releases/download/20260811-505/MinecraftClient-20260811-505-linux-x64 -O mcc

# Give MCC permission to run
RUN chmod +x mcc

# Start the bot automatically when Railway boots up!
CMD ["./mcc"]
