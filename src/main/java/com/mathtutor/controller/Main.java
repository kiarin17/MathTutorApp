package com.mathtutor.controller;

import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.stage.Stage;

import java.io.IOException;

public class Main extends Application {
    @Override
    public void start(Stage stage) throws IOException {
        Parent root = FXMLLoader.load(getClass().getResource("/views/main.fxml"));
        stage.setScene(new Scene(root, 600, 400));
        stage.setTitle("Math Tutor App");
        stage.show();
    }

    public static void main(String[] args) {
        launch(args);
    }
}