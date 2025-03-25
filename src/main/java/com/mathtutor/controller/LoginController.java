package com.mathtutor.controller;

import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.scene.control.*;
import javafx.stage.Stage;
import java.io.IOException;

public class LoginController {

    @FXML private TextField log;
    @FXML private PasswordField password;
    @FXML private Button signIn;
    @FXML private Label errorLabel;

    @FXML
    private void handleSignIn() {
        try {
            String inputLogin = log.getText().trim();
            String inputPassword = password.getText().trim();

            if (inputLogin.isEmpty() || inputPassword.isEmpty()) {
                showError("Заполните все поля!");
                return;
            }

            if ("admin".equals(inputLogin) && "admin".equals(inputPassword)) {
                openAdminPanel();
            } else {
                showError("Неверный логин или пароль");
            }
        } catch (Exception e) {
            e.printStackTrace();
            showError("Произошла ошибка: " + e.getMessage());
        }
    }

    private void openAdminPanel() throws IOException {
        // 1. Загружаем FXML админ-панели
        FXMLLoader loader = new FXMLLoader(
                getClass().getResource("/views/admin_panel.fxml")
        );
        Parent root = loader.load();

        // 2. Создаём новое окно
        Stage adminStage = new Stage();
        adminStage.setTitle("Админ-панель");
        adminStage.setScene(new Scene(root, 800, 600));
        adminStage.setResizable(true);

        // 3. Показываем новое окно
        adminStage.show();

        // 4. Закрываем текущее окно входа
        Stage loginStage = (Stage) signIn.getScene().getWindow();
        loginStage.close();
    }

    private void showError(String message) {
        errorLabel.setText(message);
        errorLabel.setVisible(true);
    }
}