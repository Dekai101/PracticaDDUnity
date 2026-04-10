package com.example.demo.components;

import java.util.List;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;

import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.TextMessage;


import java.io.IOException;

import java.util.concurrent.atomic.AtomicBoolean;

import java.util.UUID;

/**
 * Classe que representa una partida i tot el seu estat:
 *  - jugadors connectats
 *  - cua de missatges
 *  - estat intern del joc (@TODO)
 */
public class GameInstance {

    /** Les connexions dels jugadors de la partida  */
    private final List<WebSocketSession> players;

    /** Llista de missatges per gestionaro (INBOX) */
    private final BlockingQueue<GameMessage> queue = new LinkedBlockingQueue<>();

    /** Executor de fils */
    private final ExecutorService executor;

    /** Mutex per controlar que el joc només tingui 1 fil en execució */
    private final AtomicBoolean threadIsRunning = new AtomicBoolean(false);

    /** Indica si la partida està en curs (true) o ha acabat (false) */
    private volatile boolean gameActive = true;
    
    /** Id de la partida (UUID) */
    private final String id;

    public GameInstance(List<WebSocketSession> players, ExecutorService executor) {
        this.id = UUID.randomUUID().toString();
        this.players = players;
        this.executor = executor;
        System.out.println("Starting the game instance.");
    }

    /** Iniciar fil de la partida */
    public void start(){
        if(threadIsRunning.compareAndSet(false, true)){
            System.out.println("The thread has been started.");
            executor.submit(this::processLoop);
        }
    }

    /** Encuar missatge d'un player */
    public void enqueue(GameMessage message) {
        queue.offer(message); 
    }
    /** retorna l'ID de la partida */
    public String getId() {
        return id;
    }

    private void processLoop() {
        try {
            while (gameActive) {

                GameMessage msg = queue.poll(15, TimeUnit.SECONDS);

                if (msg != null) {
                    String payload = msg.payload();
                    System.out.println("Game instance: Message received");
                    if ("ping".equalsIgnoreCase(payload)) {
                        send(msg.session(), "pong");
                    }
                } else {
                    System.out.println("Game instance: timeout in message reception.");
                    broadcast("Timeout occurred");
                }

            }

        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }
    }

    /** Funció d'utilitat per enviar un missatge a una connexió concreta */
    private void send(WebSocketSession session, String text) {
        try {
            synchronized (session) {
                session.sendMessage(new TextMessage(text));
            }
        } catch (IOException e) {
            // handle disconnect
        }
    }
    /** Funció d'utilitat per enviar un missatge a tots els ususaris de la partida */
    private void broadcast(String text) {
        for (WebSocketSession player : players) {
            send(player, text);
        }
    }

    public void stop() {
        gameActive = false;
    }
}