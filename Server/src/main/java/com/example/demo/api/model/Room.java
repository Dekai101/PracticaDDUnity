package com.example.demo.api.model;

public class Room {
    private long id;
    private String name;
    private String type;
    private int level;

    public Room(long id, String name, String type, int level){
        this.id = id;
        this.name = name;
        this.type = type;
        this.level = level;
    }

    public String getType(){
        return type;
    }


    public long getId() {
        return id;
    }


    public String getName() {
        return name;
    }

    public int getLevel(){
        return level;
    }
}
