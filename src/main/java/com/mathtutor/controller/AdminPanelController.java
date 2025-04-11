package com.mathtutor.controller;

import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.layout.StackPane;
import javafx.scene.control.Button;
import javafx.event.ActionEvent;
import java.io.IOException;
import javafx.scene.Scene;  // Для смены сцены при логауте
import javafx.stage.Stage;  // Для смены сцены при логауте
import javafx.scene.layout.VBox;

public class AdminPanelController {

    @FXML
    private StackPane contentArea;

    @FXML
    private VBox navPanel;  // Панель навигации

    @FXML
    private Button homeButton;
    @FXML
    private Button studentsButton;
    @FXML
    private Button scheduleButton;
    @FXML
    private Button paymentsButton;
    @FXML
    private Button settingsButton;

    // Метод для загрузки FXML-представлений
    private void loadView(String fxmlPath) {
        try {
            Parent view = FXMLLoader.load(getClass().getResource(fxmlPath));
            contentArea.getChildren().setAll(view);
        } catch (IOException e) {
            e.printStackTrace();
            // Можно добавить обработку ошибки
        }
    }

    @FXML
    private void showMain(ActionEvent event) {
        loadView("/views/home.fxml");
        activateButton(homeButton);
    }

    @FXML
    private void showStudents(ActionEvent event) {
        loadView("/views/students.fxml");
        activateButton(studentsButton);
    }

    @FXML
    private void showSchedule(ActionEvent event) {
        loadView("/views/schedule.fxml");
        activateButton(scheduleButton);
    }

    @FXML
    private void showPayments(ActionEvent event) {
        loadView("/views/payments.fxml");
        activateButton(paymentsButton);
    }

    @FXML
    private void showSettings(ActionEvent event) {
        loadView("/views/settings.fxml");
        activateButton(settingsButton);
    }

    @FXML
    private void handleLogout(ActionEvent event) {
        try {
            Parent loginView = FXMLLoader.load(getClass().getResource("/views/login.fxml"));
            Stage stage = (Stage)((Button)event.getSource()).getScene().getWindow();
            stage.setScene(new Scene(loginView));
            stage.setTitle("Авторизация");
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    // Метод для подсветки активной кнопки
    private void activateButton(Button activeButton) {
        homeButton.getStyleClass().remove("active");
        studentsButton.getStyleClass().remove("active");
        scheduleButton.getStyleClass().remove("active");
        paymentsButton.getStyleClass().remove("active");
        settingsButton.getStyleClass().remove("active");

        activeButton.getStyleClass().add("active");
    }

    // Метод для раскрытия меню
    @FXML
    private void expandMenu() {
        navPanel.getStyleClass().remove("collapsed");
    }

    // Метод для сворачивания меню
    @FXML
    private void collapseMenu() {
        navPanel.getStyleClass().add("collapsed");
    }
}
