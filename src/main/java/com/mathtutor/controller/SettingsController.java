package com.mathtutor.controller;

import javafx.animation.FadeTransition;
import javafx.animation.ParallelTransition;
import javafx.animation.ScaleTransition;
import javafx.fxml.FXML;
import javafx.scene.control.*;
import javafx.scene.layout.VBox;
import javafx.util.Duration;

public class SettingsController {

    @FXML private TabPane tabPane;
    @FXML private VBox cardBox;
    @FXML private TextField num1, num2, num3, num4;
    @FXML private TextField cardHolderField;
    @FXML private Button savePaymentInfoButton;
    @FXML private Label saveStatusLabel;

    @FXML
    private void initialize() {
        tabPane.getSelectionModel().selectedItemProperty().addListener((obs, oldTab, newTab) -> {
            if (newTab.getText().equals("Платёжные данные")) {
                animateCard();
            }
        });

        savePaymentInfoButton.setOnAction(event -> {
            String fullCardNumber = num1.getText() + " " + num2.getText() + " " + num3.getText() + " " + num4.getText();
            String holder = cardHolderField.getText();

            if (fullCardNumber.replace(" ", "").length() == 16) {
                saveStatusLabel.setText("✅ Сохранено: " + fullCardNumber);
            } else {
                saveStatusLabel.setText("❌ Введите корректный номер карты");
            }
        });
    }

    private void animateCard() {
        cardBox.setOpacity(0);
        cardBox.setScaleX(0.95);
        cardBox.setScaleY(0.95);
        cardBox.setVisible(true); // <— убедимся, что видно

        FadeTransition fade = new FadeTransition(Duration.millis(400), cardBox);
        fade.setToValue(1.0);

        ScaleTransition scale = new ScaleTransition(Duration.millis(400), cardBox);
        scale.setToX(1.0);
        scale.setToY(1.0);

        new ParallelTransition(fade, scale).play();
    }
}
